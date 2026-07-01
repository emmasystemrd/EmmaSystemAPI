using Dapper;
using EmmaSystem.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public class SesionRepository
{
    private readonly SqlConnectionFactory _centralFactory;

    public SesionRepository(SqlConnectionFactory centralFactory)
    {
        _centralFactory = centralFactory;
    }

    /// <summary>
    /// NUEVO Y CRÍTICO: Valida la concurrencia y registra la sesión de forma ATÓMICA 
    /// utilizando un bloqueo exclusivo de aplicación en SQL Server para evitar condiciones de carrera.
    /// </summary>
    public async Task<(bool PuedeIniciar, string Mensaje)> ValidarYRegistrarSesionAtomicaAsync(
        int idUsuarioCentral, int idCliente, int? idEmpresa, string token,
        string ipAddress, string userAgent, string deviceId, string nombreEquipo,
        int maxConcurrentes, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();

        // Nos aseguramos de abrir la conexión explícitamente para manejar la transacción de Dapper
        if (conn.State != ConnectionState.Open)
        {
            if (conn is SqlConnection sqlConn)
            {
                await sqlConn.OpenAsync(ct);
            }
            else
            {
                conn.Open();
            }
        }

        using var trans = conn.BeginTransaction();

        try
        {
            // 1. Obtener un bloqueo exclusivo en memoria de SQL Server específico para este IdCliente.
            // Se usa DynamicParameters para capturar correctamente el Return Value de sp_getapplock
            string lockName = $"EmmaSystem_Lock_Cliente_{idCliente}";
            var pLock = new DynamicParameters();
            pLock.Add("@Resource", lockName);
            pLock.Add("@LockMode", "Exclusive");
            pLock.Add("@LockOwner", "Transaction");
            pLock.Add("@LockTimeout", 5000); // Espera máxima de 5 segundos si hay congestión
            pLock.Add("@ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            await conn.ExecuteAsync("sp_getapplock", pLock, transaction: trans, commandType: CommandType.StoredProcedure);
            int resLock = pLock.Get<int>("@ReturnValue");

            // Si el candado devuelve un valor negativo, significa que falló o expiró el timeout
            if (resLock < 0)
            {
                trans.Rollback();
                return (false, "No se pudo adquirir el candado de concurrencia para el inicio de sesión. Intente de nuevo.");
            }

            // 2. Limpiar sesiones colgadas/muertas sin heartbeat por más de 15 minutos ANTES de contar
            const string sqlLimpieza = @"
                UPDATE sesiones_activas
                SET Estado = 0
                WHERE IdCliente = @IdCliente
                  AND Estado = 1
                  AND UltimoActividad < DATEADD(MINUTE, -15, GETDATE());";

            await conn.ExecuteAsync(sqlLimpieza, new { IdCliente = idCliente }, transaction: trans);

            // 3. Contar las sesiones activas reales vigentes bajo el amparo del candado corporativo
            const string sqlConteo = @"
                SELECT COUNT(1)
                FROM sesiones_activas
                WHERE IdCliente = @IdCliente
                  AND Estado = 1;";

            int actuales = await conn.ExecuteScalarAsync<int>(sqlConteo, new { IdCliente = idCliente }, transaction: trans);

            // 4. Obtener el límite real directamente desde la licencia activa dentro del candado seguro
            const string sqlMax = @"
                SELECT ISNULL(p.MaxConcurrentes, 2)
                FROM clientes c
                INNER JOIN licencias l ON c.IdCliente = l.IdCliente
                INNER JOIN planes p ON l.IdPlan = p.IdPlan
                WHERE c.IdCliente = @IdCliente
                  AND l.EstadoLicencia = 1;";

            int limiteReal = await conn.ExecuteScalarAsync<int>(sqlMax, new { IdCliente = idCliente }, transaction: trans);
            if (limiteReal <= 0) limiteReal = maxConcurrentes > 0 ? maxConcurrentes : 2;

            // 5. Si ya alcanzó o superó el límite permitido por su plan, abortamos la transacción y rechazamos
            if (actuales >= limiteReal)
            {
                trans.Rollback();
                return (false,
                    $"Su plan permite un máximo de {limiteReal} sesión(es) simultánea(s). " +
                    $"Actualmente hay {actuales} sesión(es) activa(s). " +
                    $"Cierre sesión en otro dispositivo o actualice su plan para continuar.");
            }

            // 6. Registrar la nueva sesión activa dentro del bloque protegido y seguro
            const string sqlInsert = @"
                INSERT INTO sesiones_activas
                    (IdUsuarioCentral, IdCliente, IdEmpresa, Token, IPAddress, UserAgent,
                     DeviceId, NombreEquipo, FechaInicio, UltimoActividad, Estado)
                VALUES
                    (@IdUsuarioCentral, @IdCliente, @IdEmpresa, @Token, @IPAddress, @UserAgent,
                     @DeviceId, @NombreEquipo, GETDATE(), GETDATE(), 1);";

            await conn.ExecuteAsync(sqlInsert, new
            {
                IdUsuarioCentral = idUsuarioCentral,
                IdCliente = idCliente,
                IdEmpresa = idEmpresa,
                Token = token,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                DeviceId = deviceId,
                NombreEquipo = nombreEquipo
            }, transaction: trans);

            // Si todo salió bien, guardamos los cambios de forma permanente y liberamos el candado automáticamente
            trans.Commit();
            return (true, "OK");
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public async Task<int> ContarSesionesActivasAsync(int idCliente, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
        UPDATE sesiones_activas
        SET Estado = 0
        WHERE IdCliente = @IdCliente
          AND Estado = 1
          AND UltimoActividad < DATEADD(MINUTE, -15, GETDATE());

        SELECT COUNT(1)
        FROM sesiones_activas
        WHERE IdCliente = @IdCliente
          AND Estado = 1";

        return await conn.ExecuteScalarAsync<int>(sql, new { IdCliente = idCliente });
    }

    public async Task<bool> ExisteSesionDispositivoAsync(int idCliente, string deviceId, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
        SELECT COUNT(1)
        FROM sesiones_activas
        WHERE IdCliente = @IdCliente
          AND DeviceId = @DeviceId
          AND Estado = 1
          AND UltimoActividad >= DATEADD(MINUTE, -15, GETDATE())";

        return await conn.ExecuteScalarAsync<int>(sql, new { IdCliente = idCliente, DeviceId = deviceId }) > 0;
    }

    public async Task CerrarSesionDispositivoAsync(int idCliente, string deviceId, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
        UPDATE sesiones_activas
        SET Estado = 0
        WHERE IdCliente = @IdCliente
          AND DeviceId = @DeviceId";

        await conn.ExecuteAsync(sql, new { IdCliente = idCliente, DeviceId = deviceId });
    }

    public async Task RegistrarSesionAsync(int idUsuarioCentral, int idCliente, int? idEmpresa,
        string token, string ipAddress, string userAgent, string deviceId, string nombreEquipo,
        CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
        INSERT INTO sesiones_activas
            (IdUsuarioCentral, IdCliente, IdEmpresa, Token, IPAddress, UserAgent,
             DeviceId, NombreEquipo, FechaInicio, UltimoActividad, Estado)
        VALUES
            (@IdUsuarioCentral, @IdCliente, @IdEmpresa, @Token, @IPAddress, @UserAgent,
             @DeviceId, @NombreEquipo, GETDATE(), GETDATE(), 1)";

        await conn.ExecuteAsync(sql, new
        {
            IdUsuarioCentral = idUsuarioCentral,
            IdCliente = idCliente,
            IdEmpresa = idEmpresa,
            Token = token,
            IPAddress = ipAddress,
            UserAgent = userAgent,
            DeviceId = deviceId,
            NombreEquipo = nombreEquipo
        });
    }

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

    public async Task CerrarSesionAsync(string token, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
            UPDATE sesiones_activas
            SET Estado = 0
            WHERE Token = @Token";

        await conn.ExecuteAsync(sql, new { Token = token });
    }

    public async Task CerrarTodasLasSesionesAsync(int idUsuarioCentral, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
            UPDATE sesiones_activas
            SET Estado = 0
            WHERE IdUsuarioCentral = @IdUsuarioCentral AND Estado = 1";

        await conn.ExecuteAsync(sql, new { IdUsuarioCentral = idUsuarioCentral });
    }

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

    public async Task<int> ContarEmpresasActivasAsync(int idCliente, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        const string sql = @"
            SELECT COUNT(1)
            FROM empresas_contratadas
            WHERE IdCliente = @IdCliente AND Estado = 1";

        return await conn.ExecuteScalarAsync<int>(sql, new { IdCliente = idCliente });
    }

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