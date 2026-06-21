using Dapper;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class PermissionRepository
{
    private readonly ITenantConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public PermissionRepository(
        ITenantConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<bool> HasPermissionAsync(int idAcceso, int idModulo, int idOperacion, CancellationToken ct)
    {
        const string sql = @"
            DECLARE @bit BIT = 0;
            DECLARE @IdrolTarget INT;
            SELECT TOP 1 @IdrolTarget = Idrol FROM dbo.Rol WHERE Idmodulo = @Idmodulo AND Idoperacion = @Idoperacion;
            IF @IdrolTarget IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Permiso WHERE Idacceso = @Idacceso AND Idrol = @IdrolTarget)
                SET @bit = 1;
            SELECT @bit;";

        using var conn = await _connectionFactory.CrearConexionAsync(_tenantContext.EmpresaId);

        return await conn.ExecuteScalarAsync<bool>(sql, new { Idacceso = idAcceso, Idmodulo = idModulo, Idoperacion = idOperacion });
    }
}