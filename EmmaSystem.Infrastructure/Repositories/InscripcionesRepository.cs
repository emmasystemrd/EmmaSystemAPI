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
        p.Add("@Fecha1", fecha1 ?? DateTime.MinValue);
        p.Add("@Fecha2", fecha2 ?? DateTime.MaxValue);
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

        return await conn.QueryFirstOrDefaultAsync<InscripcionDetalleDto>(
            new CommandDefinition("[dbo].[BuscarInscripcionID]",
                new { TextoBuscar = idInscripcion },
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
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

    public async Task<int> InsertAsync(InscripcionSaveDto dto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@Codigo", dto.Codigo ?? "");
        p.Add("@Fecha", dto.Fecha);
        p.Add("@IdEstudiante", dto.IdEstudiante);
        p.Add("@Idcurso", dto.Idcurso);
        p.Add("@Fecha1", dto.Fecha1);
        p.Add("@Fecha2", dto.Fecha2);
        p.Add("@Idinstructor", dto.Idinstructor);
        p.Add("@Idlogin", dto.Idlogin);
        p.Add("@IsFacturaAutomatica", dto.IsFacturaAutomatica);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[InsertarInscripcion]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return await conn.ExecuteScalarAsync<int>(
            "SELECT CAST(SCOPE_IDENTITY() AS INT)",
            commandType: CommandType.Text);
    }

    public async Task UpdateAsync(int idInscripcion, InscripcionSaveDto dto, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@IdInscripcion", idInscripcion);
        p.Add("@Fecha", dto.Fecha);
        p.Add("@IdEstudiante", dto.IdEstudiante);
        p.Add("@Idcurso", dto.Idcurso);
        p.Add("@Idinstructor", dto.Idinstructor);
        p.Add("@Fecha1", dto.Fecha1);
        p.Add("@Fecha2", dto.Fecha2);
        p.Add("@IsFacturaAutomatica", dto.IsFacturaAutomatica);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[ModificarInscripcion]", p,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
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