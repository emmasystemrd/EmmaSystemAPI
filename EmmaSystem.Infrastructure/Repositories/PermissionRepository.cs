using Dapper;
using EmmaSystem.Infrastructure.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class PermissionRepository
{
    private readonly SqlConnectionFactory _factory;
    public PermissionRepository(SqlConnectionFactory factory) => _factory = factory;

    public async Task<bool> HasPermissionAsync(int idAcceso, int idModulo, int idOperacion, CancellationToken ct)
    {
        const string sql = @"
            DECLARE @bit BIT = 0;
            DECLARE @IdrolTarget INT;
            SELECT TOP 1 @IdrolTarget = Idrol FROM dbo.Rol WHERE Idmodulo = @Idmodulo AND Idoperacion = @Idoperacion;
            IF @IdrolTarget IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Permiso WHERE Idacceso = @Idacceso AND Idrol = @IdrolTarget)
                SET @bit = 1;
            SELECT @bit;";
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { Idacceso = idAcceso, Idmodulo = idModulo, Idoperacion = idOperacion });
    }
}