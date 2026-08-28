using Dapper;
using EmmaSystem.Application.DTOs.Reportes;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class ReportesEstudiantesRepository : IReportesEstudiantesRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public ReportesEstudiantesRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<IReadOnlyList<EstudianteReporteDto>> GetReporteEstudiantesAsync(
        string textoBuscar, string? estadoEstudiante, int? idCurso, string? estadoInscripcion,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var p = new DynamicParameters();
        p.Add("@TextoBuscar", textoBuscar ?? "");
        p.Add("@EstadoEstudiante", string.IsNullOrEmpty(estadoEstudiante) ? (object)DBNull.Value : estadoEstudiante);
        p.Add("@IdCurso", idCurso ?? (object)DBNull.Value);
        p.Add("@EstadoInscripcion", string.IsNullOrEmpty(estadoInscripcion) ? (object)DBNull.Value : estadoInscripcion);

        var result = await conn.QueryAsync<EstudianteReporteDto>(
            new CommandDefinition("[dbo].[R_Listado_Estudiantes_Filtrado]", p,
            commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }
}