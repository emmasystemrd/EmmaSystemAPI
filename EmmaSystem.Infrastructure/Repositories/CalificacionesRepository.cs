using Dapper;
using EmmaSystem.Application.DTOs.Estudiante;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class CalificacionesRepository : ICalificacionesRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public CalificacionesRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<IReadOnlyList<EstudianteCalificacionDto>> GetEstudiantesByCursoAsync(
        int idCurso, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        var result = await conn.QueryAsync<EstudianteCalificacionDto>(
            new CommandDefinition("[dbo].[ObtenerEstudiantesPorCurso]",
            new { IdCurso = idCurso },
            commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return result.ToList();
    }

    // ✅ CAMBIADO: Ahora actualiza la inscripción
    public async Task ActualizarEstadoInscripcionAsync(
        int idInscripcion, string nuevoEstado, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        await conn.ExecuteAsync(
            new CommandDefinition("[dbo].[ActualizarEstadoInscripcion]",
            new { IdInscripcion = idInscripcion, NuevoEstado = nuevoEstado },
            commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }
}