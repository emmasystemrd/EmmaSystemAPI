using Dapper;
using EmmaSystem.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace EmmaSystem.Infrastructure.Repositories;

public class SesionRepository
{
    private readonly SqlConnectionFactory _centralFactory;

    public SesionRepository(SqlConnectionFactory centralFactory)
    {
        _centralFactory = centralFactory;
    }

    /// <summary>
    /// Cuenta las sesiones activas de un cliente específico
    /// </summary>
    public async Task<int> ContarSesionesActivasAsync(int idCliente, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
            SELECT COUNT(1) 
            FROM sesiones_activas 
            WHERE IdCliente = @IdCliente 
              AND Estado = 1";

        return await conn.ExecuteScalarAsync<int>(sql, new { IdCliente = idCliente });
    }

    /// <summary>
    /// Obtiene el MaxConcurrentes del plan del cliente
    /// </summary>
    public async Task<int> GetMaxConcurrentesAsync(int idCliente, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
            SELECT ISNULL(p.MaxConcurrentes, 2)
            FROM clientes c
            INNER JOIN licencias l ON c.IdCliente = l.IdCliente
            INNER JOIN planes p ON l.IdPlan = p.IdPlan
            WHERE c.IdCliente = @IdCliente
              AND l.EstadoLicencia = 1";

        return await conn.ExecuteScalarAsync<int>(sql, new { IdCliente = idCliente });
    }

    /// <summary>
    /// Registra una nueva sesión activa
    /// </summary>
    public async Task RegistrarSesionAsync(int idUsuarioCentral, int idCliente, int? idEmpresa,
        string token, string ipAddress, string userAgent, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
            INSERT INTO sesiones_activas 
                (IdUsuarioCentral, IdCliente, IdEmpresa, Token, IPAddress, UserAgent, FechaInicio, UltimoActividad, Estado)
            VALUES 
                (@IdUsuarioCentral, @IdCliente, @IdEmpresa, @Token, @IPAddress, @UserAgent, GETDATE(), GETDATE(), 1)";

        await conn.ExecuteAsync(sql, new
        {
            IdUsuarioCentral = idUsuarioCentral,
            IdCliente = idCliente,
            IdEmpresa = idEmpresa,
            Token = token,
            IPAddress = ipAddress,
            UserAgent = userAgent
        });
    }

    /// <summary>
    /// Actualiza la última actividad de una sesión (heartbeat)
    /// </summary>
    public async Task<bool> ActualizarUltimoActividadAsync(string token, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
            UPDATE sesiones_activas 
            SET UltimoActividad = GETDATE()
            WHERE Token = @Token AND Estado = 1";

        var affected = await conn.ExecuteAsync(sql, new { Token = token });
        return affected > 0;
    }

    /// <summary>
    /// Cierra una sesión específica (logout)
    /// </summary>
    public async Task CerrarSesionAsync(string token, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
            UPDATE sesiones_activas 
            SET Estado = 0 
            WHERE Token = @Token";

        await conn.ExecuteAsync(sql, new { Token = token });
    }

    /// <summary>
    /// Cierra todas las sesiones de un usuario (útil para logout forzoso)
    /// </summary>
    public async Task CerrarTodasLasSesionesAsync(int idUsuarioCentral, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
            UPDATE sesiones_activas 
            SET Estado = 0 
            WHERE IdUsuarioCentral = @IdUsuarioCentral AND Estado = 1";

        await conn.ExecuteAsync(sql, new { IdUsuarioCentral = idUsuarioCentral });
    }

    /// <summary>
    /// Limpia sesiones inactivas por más de X minutos
    /// </summary>
    public async Task<int> LimpiarSesionesInactivasAsync(int minutosInactividad = 30, CancellationToken ct = default)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
            UPDATE sesiones_activas 
            SET Estado = 0 
            WHERE UltimoActividad < DATEADD(MINUTE, -@Minutos, GETDATE()) 
              AND Estado = 1";

        return await conn.ExecuteAsync(sql, new { Minutos = minutosInactividad });
    }

    /// <summary>
    /// Cuenta las empresas activas de un cliente (para validar MaxEmpresas)
    /// </summary>
    public async Task<int> ContarEmpresasActivasAsync(int idCliente, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
            SELECT COUNT(1) 
            FROM empresas_contratadas 
            WHERE IdCliente = @IdCliente AND Estado = 1";

        return await conn.ExecuteScalarAsync<int>(sql, new { IdCliente = idCliente });
    }

    /// <summary>
    /// Obtiene el MaxEmpresas del plan del cliente
    /// </summary>
    public async Task<int> GetMaxEmpresasAsync(int idCliente, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
            SELECT ISNULL(p.MaxEmpresas, 1)
            FROM clientes c
            INNER JOIN licencias l ON c.IdCliente = l.IdCliente
            INNER JOIN planes p ON l.IdPlan = p.IdPlan
            WHERE c.IdCliente = @IdCliente
              AND l.EstadoLicencia = 1";

        return await conn.ExecuteScalarAsync<int>(sql, new { IdCliente = idCliente });
    }
}