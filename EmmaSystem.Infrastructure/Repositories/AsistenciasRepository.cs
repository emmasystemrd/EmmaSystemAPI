using Dapper;
using EmmaSystem.Application.DTOs.Asistencia;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class AsistenciasRepository : IAsistenciasRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public AsistenciasRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<List<EstudianteAsistenciaDto>> GetEstudiantesParaAsistenciaAsync(
        int idAsistencia, DateTime fecha, int idCurso, int? idDetalleCurso, int idInstructor, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

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
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Fecha", fecha.Date);
        p.Add("@Idcurso", idCurso);
        p.Add("@Iddetall_Curso", idDetalleCurso);
        p.Add("@Idinstructor", idInstructor);

        return await conn.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition("[dbo].[spGenerarId_Asistencia]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task<int> SaveAsistenciaAsync(AsistenciaSaveDto dto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        conn.Open();
        using var transaction = conn.BeginTransaction();

        try
        {
            int idAsistencia = dto.IdAsistencia ?? 0;

            if (idAsistencia <= 0)
            {
                var pHeader = new DynamicParameters();
                pHeader.Add("@Fecha", dto.Fecha);
                pHeader.Add("@Idcurso", dto.Idcurso);
                pHeader.Add("@Iddetalle_Curso", dto.Iddetalle_Curso, DbType.Int32);
                pHeader.Add("@Idlogin", dto.Idlogin);
                pHeader.Add("@Idinstructor", dto.Idinstructor);

                idAsistencia = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition("[dbo].[InsertarAsistencia]", pHeader,
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: ct));
            }

            if (idAsistencia <= 0)
            {
                throw new Exception("No se pudo obtener el ID de asistencia. Verifique que el SP InsertarAsistencia devuelva el ID.");
            }

            foreach (var detalle in dto.Detalles)
            {
                if (string.IsNullOrWhiteSpace(detalle.Asistencia))
                    continue;

                if (detalle.Iddetalle > 0)
                {
                    var pUpdate = new DynamicParameters();
                    pUpdate.Add("@Iddetalle", detalle.Iddetalle);
                    pUpdate.Add("@Asistencia", detalle.Asistencia);

                    await conn.ExecuteAsync(
                        new CommandDefinition("[dbo].[Editar_Detalle_Asistencia]", pUpdate,
                            transaction: transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));
                }
                else
                {
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
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

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