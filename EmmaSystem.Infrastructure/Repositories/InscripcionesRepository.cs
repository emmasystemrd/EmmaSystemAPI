using Dapper;
using EmmaSystem.Application.DTOs.Estudiante;
using EmmaSystem.Application.DTOs.Inscripcion;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class InscripcionesRepository : IInscripcionesRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public InscripcionesRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<IReadOnlyList<InscripcionListadoDto>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<InscripcionListadoDto>(
            new CommandDefinition("[dbo].[MostrarInscripcion]",
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }

    public async Task<IReadOnlyList<InscripcionListadoDto>> SearchAsync(
        DateTime? fecha1, DateTime? fecha2, bool isFecha, string texto, string columna, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);
        var columnasValidas = new[] { "e.Nombres", "e.Apellido1", "e.Apellido2", "e.Num_Documento", "u.Curso", "i.Codigo" };
        if (!string.IsNullOrEmpty(columna) && !columnasValidas.Contains(columna))
            columna = "e.Nombres";
        else if (string.IsNullOrEmpty(columna))
            columna = "e.Nombres";

        var p = new DynamicParameters();

        // ✅ CORREGIDO: SQL Server DATETIME mínimo es 1753-01-01. DateTime.MinValue causa SqlDateTime overflow.
        p.Add("@Fecha1", fecha1 ?? new DateTime(1900, 1, 1));
        p.Add("@Fecha2", fecha2 ?? new DateTime(9999, 12, 31));

        p.Add("@IsFecha", isFecha ? 1 : 0);
        p.Add("@TextoBuscar", string.IsNullOrEmpty(texto) ? "" : texto);
        p.Add("@Columna", columna);

        var result = await conn.QueryAsync<InscripcionListadoDto>(
            new CommandDefinition("[dbo].[BuscarInscripcion_columna]", p,
            commandType: CommandType.StoredProcedure, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<InscripcionDetalleDto?> GetByIdAsync(int idInscripcion, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
        SELECT
            i.IdInscripcion,
            i.Codigo,
            c.Codigo AS Codigo_Curso,
            c.Nivel,
            i.IsFacturaAutomatica,
            c.Valor_Inscripcion,
            c.Valor_Mensual,
            i.Fecha,
            i.Fecha1,
            i.Fecha2,
            c.Horario,
            e.Codigo AS Codigo_Estudiante,
            i.IdEstudiante AS Idestudiante,
            i.Idcurso,
            i.Idinstructor,
            ISNULL(emp.Num_Doc, '') AS Codigo_Facilitador
        FROM Inscripcion i
        INNER JOIN Curso c ON c.Idcurso = i.Idcurso
        INNER JOIN Estudiante e ON e.Idestudiante = i.IdEstudiante
        LEFT JOIN Empleado emp ON emp.Idempleado = i.Idinstructor
        WHERE i.IdInscripcion = @IdInscripcion;";

        return await conn.QueryFirstOrDefaultAsync<InscripcionDetalleDto>(
            new CommandDefinition(sql, new { IdInscripcion = idInscripcion }, cancellationToken: ct));
    }

    public async Task<InscripcionImpresionDto?> GetPrintDataAsync(int idInscripcion, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        return await conn.QueryFirstOrDefaultAsync<InscripcionImpresionDto>(
            new CommandDefinition("[dbo].[ImprimirInscripcion]",
                new { Idinscripcion = idInscripcion },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task<EstudianteDetalleDto?> GetEstudianteByCodigoAsync(string textoBuscar, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition("[dbo].[BuscarEstudiante_Codigo]",
                new { TextoBuscar = textoBuscar },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        if (row == null) return null;

        return new EstudianteDetalleDto
        {
            IdEstudiante = row.Idestudiante,
            Codigo = row.Codigo ?? "",
            Nombres = row.Nombres ?? "",
            Apellido1 = row.Apellido1 ?? "",
            Apellido2 = row.Apellido2 ?? "",
            Estudiante = row.Estudiante ?? "",
            Num_Documento = row.Num_Documento ?? "",
            Telefono = row.Telefono ?? "",
            Codigo_Padre = row.Codigo_Padre ?? ""
        };
    }

    public async Task<int> InsertAsync(
     InscripcionSaveDto dto,
     CancellationToken ct = default)
    {
        using var conn = await _connectionFactory
            .CrearConexionAsync(_tenantContext.EmpresaId);

        var fechasCurso = await conn.QueryFirstOrDefaultAsync<(
            DateTime? Fecha_Inicio,
            DateTime? Fecha_Final
        )>(
            new CommandDefinition(
                @"SELECT Fecha_Inicio, Fecha_Final
              FROM Curso
              WHERE Idcurso = @Idcurso
                AND Estado = 'A'",
                new { dto.Idcurso },
                cancellationToken: ct));

        if (fechasCurso == default)
            throw new InvalidOperationException(
                "El curso seleccionado no existe o no está activo.");

        var p = new DynamicParameters();

        p.Add("@Codigo", dto.Codigo ?? "");
        p.Add("@Fecha", dto.Fecha);
        p.Add("@IdEstudiante", dto.IdEstudiante);
        p.Add("@Idcurso", dto.Idcurso);

        // Fechas tomadas automáticamente del curso
        p.Add("@Fecha1", fechasCurso.Fecha_Inicio);
        p.Add("@Fecha2", fechasCurso.Fecha_Final);

        p.Add("@Idinstructor", dto.Idinstructor);
        p.Add("@Idlogin", dto.Idlogin);
        p.Add("@IsFacturaAutomatica", dto.IsFacturaAutomatica);

        var id = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "[dbo].[InsertarInscripcion]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return id;
    }


    public async Task UpdateAsync(
        int idInscripcion,
        InscripcionSaveDto dto,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory
            .CrearConexionAsync(_tenantContext.EmpresaId);

        var fechasCurso = await conn.QueryFirstOrDefaultAsync<(
            DateTime? Fecha_Inicio,
            DateTime? Fecha_Final
        )>(
            new CommandDefinition(
                @"SELECT Fecha_Inicio, Fecha_Final
              FROM Curso
              WHERE Idcurso = @Idcurso
                AND Estado = 'A'",
                new { dto.Idcurso },
                cancellationToken: ct));

        if (fechasCurso == default)
            throw new InvalidOperationException(
                "El curso seleccionado no existe o no está activo.");

        var p = new DynamicParameters();

        p.Add("@IdInscripcion", idInscripcion);
        p.Add("@Fecha", dto.Fecha);
        p.Add("@IdEstudiante", dto.IdEstudiante);
        p.Add("@Idcurso", dto.Idcurso);
        p.Add("@Idinstructor", dto.Idinstructor);

        // Fechas tomadas automáticamente del curso
        p.Add("@Fecha1", fechasCurso.Fecha_Inicio);
        p.Add("@Fecha2", fechasCurso.Fecha_Final);

        p.Add("@IsFacturaAutomatica", dto.IsFacturaAutomatica);

        await conn.ExecuteAsync(
            new CommandDefinition(
                "[dbo].[ModificarInscripcion]",
                p,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task DeleteAsync(int idInscripcion, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[EliminarInscripcion]",
                new { IdInscripcion = idInscripcion },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }
}