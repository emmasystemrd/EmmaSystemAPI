using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace EmmaSystem.Infrastructure.Data;

/// <summary>
/// Factoría de conexiones SQL Server para Dapper.
/// Se registra como Scoped en DI para que cada request tenga su propia conexión.
/// 
/// NOTA: Retornamos SqlConnection (clase concreta) en lugar de IDbConnection (interfaz antigua)
/// porque IDbConnection NO tiene métodos asíncronos (OpenAsync, ExecuteReaderAsync).
/// SqlConnection hereda de DbConnection que SÍ los incluye.
/// Dapper funciona perfectamente con SqlConnection porque implementa IDbConnection.
/// </summary>
public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("EmmaSystem")
            ?? throw new InvalidOperationException("Connection string 'EmmaSystem' not found in appsettings.json");
    }

    /// <summary>
    /// Crea una nueva conexión SQL. El caller es responsable de hacer Dispose() via using.
    /// </summary>
    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}