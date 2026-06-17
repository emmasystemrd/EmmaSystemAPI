using Microsoft.Data.SqlClient;

namespace EmmaSystem.Application.Interfaces;

/// <summary>
/// Factory para crear conexiones a bases de datos de empresas (multi-tenant)
/// </summary>
public interface ITenantConnectionFactory
{
    /// <summary>
    /// Crea una conexión SQL a la base de datos de una empresa específica
    /// </summary>
    /// <param name="empresaId">ID de la empresa contratada</param>
    /// <returns>SqlConnection abierta y lista para usar</returns>
    /// <exception cref="InvalidOperationException">Si la empresa está inactiva</exception>
    Task<SqlConnection> CrearConexionAsync(int empresaId);
}