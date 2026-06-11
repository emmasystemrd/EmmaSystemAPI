using Dapper;
using EmmaSystem.Application.DTOs.Asistencia;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class AsistenciasRepository : IAsistenciasRepository
{
    private readonly SqlConnectionFactory _factory;

    public AsistenciasRepository(SqlConnectionFactory factory) => _factory = factory;

    public async Task<List<EstudianteAsistenciaDto>> GetEstudiantesParaAsistenciaAsync(
        int idAsistencia, DateTime fecha, int idCurso, int? idDetalleCurso, int idInstructor, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        // Tu SP espera 0 en lugar de NULL para Iddetalle_Curso e Idasistencia
        var p = new DynamicParameters();
        p.Add("@Idasistencia", idAsistencia <= 0 ? 0 : idAsistencia);
        p.Add("@Fecha", fecha.Date);
        p.Add("@Idcurso", idCurso);
        p.Add("@Iddetalle_Curso", idDetalleCurso ?? 0);
        p.Add("@Idinstructor", idInstructor);

        var result = await conn.QueryAsync<EstudianteAsistenciaDto>(
            new CommandDefinition("[dbo].[spCargar_Estudiantes_Asistencia]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }

    public async Task<int?> GetExistingAsistenciaIdAsync(
        DateTime fecha, int idCurso, int? idDetalleCurso, int idInstructor, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@Fecha", fecha.Date);
        p.Add("@Idcurso", idCurso);
        // ⚠️ Nota: Tu SP tiene un typo en el nombre del parámetro: @Iddetall_Curso (sin la 'e' final)
        p.Add("@Iddetall_Curso", idDetalleCurso);
        p.Add("@Idinstructor", idInstructor);

        return await conn.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition("[dbo].[spGenerarId_Asistencia]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task<int> SaveAsistenciaAsync(AsistenciaSaveDto dto, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        conn.Open();

        using var transaction = conn.BeginTransaction();

        try
        {
            int idAsistencia = dto.IdAsistencia ?? 0;

            // 1. Si no existe ID, crear la cabecera de asistencia
            if (idAsistencia <= 0)
            {
                var pHeader = new DynamicParameters();
                pHeader.Add("@Fecha", dto.Fecha);
                pHeader.Add("@Idcurso", dto.Idcurso);
                pHeader.Add("@Iddetalle_Curso", dto.Iddetalle_Curso, DbType.Int32);
                pHeader.Add("@Idlogin", dto.Idlogin);
                pHeader.Add("@Idinstructor", dto.Idinstructor);

                // ✅ CORRECCIÓN: Ejecutar el SP y obtener el ID devuelto
                idAsistencia = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition("[dbo].[InsertarAsistencia]", pHeader,
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: ct));
            }

            // ✅ VERIFICACIÓN: Asegurarse de que el ID sea válido antes de insertar detalles
            if (idAsistencia <= 0)
            {
                throw new Exception("No se pudo obtener el ID de asistencia. Verifique que el SP InsertarAsistencia devuelva el ID.");
            }

            // 2. Procesar cada detalle (estudiante)
            foreach (var detalle in dto.Detalles)
            {
                if (string.IsNullOrWhiteSpace(detalle.Asistencia))
                    continue;

                if (detalle.Iddetalle > 0)
                {
                    // Actualizar detalle existente
                    var pUpdate = new DynamicParameters();
                    pUpdate.Add("@Iddetalle", detalle.Iddetalle);
                    pUpdate.Add("@Asistencia", detalle.Asistencia);

                    await conn.ExecuteAsync(
                        new CommandDefinition("[dbo].[Editar_Detalle_Asistencia]", pUpdate,
                            transaction: transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));
                }
                else
                {
                    // Insertar nuevo detalle
                    var pInsert = new DynamicParameters();
                    pInsert.Add("@Idasistencia", idAsistencia);
                    pInsert.Add("@Idestudiante", detalle.Idestudiante);
                    pInsert.Add("@Asistencia", detalle.Asistencia);

                    await conn.ExecuteAsync(
                        new CommandDefinition("[dbo].[Insertar_Detalle_Asistencia]", pInsert,
                            transaction: transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));
                }
            }

            transaction.Commit();
            return idAsistencia;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<List<AsistenciaMatrixDto>> GetMatrixAsistenciaAsync(
        DateTime fecha1, DateTime fecha2, int idCurso, int? idDetalleCurso, int idInstructor, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@Fecha1", fecha1.Date);
        p.Add("@Fecha2", fecha2.Date);
        p.Add("@Idcurso", idCurso);
        p.Add("@Iddetalle_Curso", idDetalleCurso);
        p.Add("@Idinstructor", idInstructor);

        var result = await conn.QueryAsync<AsistenciaMatrixDto>(
            new CommandDefinition("[dbo].[RAsistencia_Estudiante]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }
}