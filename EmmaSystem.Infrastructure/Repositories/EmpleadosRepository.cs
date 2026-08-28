using Dapper;
using EmmaSystem.Application.DTOs.Empleado;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class EmpleadosRepository : IEmpleadosRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public EmpleadosRepository(ITenantConnectionFactory connectionFactory, ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<IReadOnlyList<InstructorDto>> GetInstructoresActivosAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        const string sql = @"
        SELECT
            Idempleado,
            LTRIM(RTRIM(Nombres + ' ' + Apellido1 + ' ' + ISNULL(Apellido2, ''))) AS NombreCompleto,
            ISNULL(Num_Doc, '') AS NumDoc
        FROM Empleado
        WHERE ISNULL(Estado, 'A') <> 'E'
        ORDER BY Nombres, Apellido1";

        var result = await conn.QueryAsync<InstructorDto>(
            new CommandDefinition(sql, commandType: CommandType.Text, cancellationToken: ct));

        return result.AsList();
    }
}