using Dapper;
using EmmaSystem.Application.DTOs.Auth;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using EmmaSystem.Infrastructure.Security;
using Microsoft.Extensions.Caching.Memory;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

/// <summary>
/// Fila retornada por el SP/query de validación de usuario en BD de empresa
/// </summary>
public class SpLoginRow
{
    public int Idusuario { get; set; }
    public string Nombres { get; set; } = default!;
    public int Idacceso { get; set; }
    public decimal Max_Descuento { get; set; }
    public decimal Max_Credito { get; set; }
    public string NombrePuesto { get; set; } = default!;
    public byte[]? Foto { get; set; }
    public string Estado { get; set; } = default!;
    public string NombreUsuario { get; set; } = default!;
    public string Empresa { get; set; } = default!;
    public int Idempleado { get; set; }
    public int Idempresa { get; set; }
}

/// <summary>
/// Fila para mapeo de usuarios centrales desde EmmaSystemCentral
/// ✅ CAMBIO: internal → public para resolver error de accesibilidad
/// </summary>
public class UsuarioCentralRow
{
    public int IdUsuarioCentral { get; set; }
    public int IdCliente { get; set; }
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string NombreCompleto { get; set; } = default!;
    public bool EsSuperAdmin { get; set; }
    public int Estado { get; set; }
}

public sealed class AuthRepository
{
    private readonly SqlConnectionFactory _centralFactory;
    private readonly ITenantConnectionFactory _tenantFactory;
    private readonly IMemoryCache _cache;

    public AuthRepository(
        SqlConnectionFactory centralFactory,
        ITenantConnectionFactory tenantFactory,
        IMemoryCache cache)
    {
        _centralFactory = centralFactory ?? throw new ArgumentNullException(nameof(centralFactory));
        _tenantFactory = tenantFactory ?? throw new ArgumentNullException(nameof(tenantFactory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    // ──────────────────────────────────────────────
    // LOGIN CENTRAL (EmmaSystemCentral)
    // ──────────────────────────────────────────────

    public async Task<UsuarioCentralRow?> LoginCentralAsync(string email, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();

        const string sql = @"
            SELECT 
                uc.IdUsuarioCentral,
                uc.IdCliente,
                uc.Email,
                uc.PasswordHash,
                uc.NombreCompleto,
                uc.EsSuperAdmin,
                uc.Estado
            FROM usuarios_central uc
            INNER JOIN clientes c ON uc.IdCliente = c.IdCliente
            WHERE uc.Email = @Email
              AND uc.Estado = 1
              AND c.Estado = 1";

        return await conn.QueryFirstOrDefaultAsync<UsuarioCentralRow>(
            new CommandDefinition(sql, new { Email = email.Trim().ToLower() }, cancellationToken: ct));
    }

    public async Task<UsuarioCentralRow?> GetUsuarioCentralByIdAsync(int idUsuarioCentral, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();

        const string sql = @"
            SELECT 
                IdUsuarioCentral,
                IdCliente,
                Email,
                PasswordHash,
                NombreCompleto,
                EsSuperAdmin,
                Estado
            FROM usuarios_central
            WHERE IdUsuarioCentral = @Id AND Estado = 1";

        return await conn.QueryFirstOrDefaultAsync<UsuarioCentralRow>(
            new CommandDefinition(sql, new { Id = idUsuarioCentral }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<EmpresaDisponibleDto>> GetEmpresasByClienteAsync(int idCliente, CancellationToken ct)
    {
        var cacheKey = $"empresas_cliente_{idCliente}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<EmpresaDisponibleDto>? cached) && cached is not null)
            return cached;

        using var conn = _centralFactory.CreateConnection();

        const string sql = @"
            SELECT 
                IdEmpresa,
                NombreEmpresa,
                EsEmpresaDefault AS EsDefault
            FROM empresas_contratadas
            WHERE IdCliente = @IdCliente
              AND Estado = 1
            ORDER BY EsEmpresaDefault DESC, NombreEmpresa";

        var result = await conn.QueryAsync<EmpresaDisponibleDto>(
            new CommandDefinition(sql, new { IdCliente = idCliente }, cancellationToken: ct));

        var list = result.AsList();
        _cache.Set(cacheKey, list, TimeSpan.FromMinutes(5));
        return list;
    }

    public async Task<bool> ValidarEmpresaDeClienteAsync(int idCliente, int idEmpresa, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(1)
            FROM empresas_contratadas
            WHERE IdCliente = @IdCliente
              AND IdEmpresa = @IdEmpresa
              AND Estado = 1";

        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { IdCliente = idCliente, IdEmpresa = idEmpresa }, cancellationToken: ct));

        return count > 0;
    }

    public async Task UpdateUltimoAccesoCentralAsync(int idUsuarioCentral, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();

        const string sql = "UPDATE usuarios_central SET UltimoAcceso = GETUTCDATE() WHERE IdUsuarioCentral = @Id";

        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = idUsuarioCentral }, cancellationToken: ct));
    }

    // ──────────────────────────────────────────────
    // LOGIN DE EMPRESA (BD específica del tenant)
    // ──────────────────────────────────────────────

    public async Task<SpLoginRow?> LoginEmpresaAsync(string usuario, string password, int idEmpresa, CancellationToken ct)
    {
        string usuarioEncriptado = CryptoHelper.EncryptUsername(usuario);

        using var conn = await _tenantFactory.CrearConexionAsync(idEmpresa);

        const string validationSql = @"
            SELECT 
                u.Idusuario,
                LTRIM(t.Nombres + ' ' + t.Apellido1 + ' ' + t.Apellido2) AS Nombres,
                u.Idacceso,
                u.Max_Descuento,
                u.Max_Credito,
                p.Nombre AS NombrePuesto,
                t.Foto,
                u.Estado,
                u.Nombre AS NombreUsuario,
                e.Nombre AS Empresa,
                t.Idempleado,
                u.Idempresa
            FROM Usuario u 
            INNER JOIN Empleado t ON u.Idempleado = t.Idempleado
            INNER JOIN Puesto p ON p.Idpuesto = t.Idpuesto
            INNER JOIN Empresa e ON e.Idempresa = u.Idempresa
            WHERE u.Nombre = @usuario 
              AND CONVERT(varchar(20), DECRYPTBYPASSPHRASE('Diosesbueno@7', u.Clave)) = @password 
              AND u.Idempresa = 1;";

        var parameters = new DynamicParameters();
        parameters.Add("@usuario", usuarioEncriptado, DbType.String);
        parameters.Add("@password", password, DbType.String);
        //parameters.Add("@Idempresa", idEmpresa, DbType.Int32);

        var user = await conn.QueryFirstOrDefaultAsync<SpLoginRow>(
            new CommandDefinition(validationSql, parameters, cancellationToken: ct));

        if (user is null)
            return null;

        const string historialSql = @"
            INSERT INTO Historial (idusuario, tipo, fecha, descripcion, idempresa)
            VALUES (
                @Idusuario,
                'Sesión',
                GETDATE(),
                'El usuario ha iniciado sesión correctamente en fecha: ' + FORMAT(GETDATE(), 'dd/MM/yyyy HH:mm:ss'),
                1
            );";

        await conn.ExecuteAsync(
            new CommandDefinition(
                historialSql,
                new { user.Idusuario, Idempresa = idEmpresa },
                cancellationToken: ct));

        return user;
    }

    public async Task<IReadOnlyList<int>> GetRolesByAccesoAsync(int idAcceso, int idEmpresa, CancellationToken ct)
    {
        using var conn = await _tenantFactory.CrearConexionAsync(idEmpresa);

        const string sql = @"
            SELECT Idrol
            FROM dbo.Permiso
            WHERE Idacceso = @Idacceso;";

        var roles = await conn.QueryAsync<int>(
            new CommandDefinition(sql, new { Idacceso = idAcceso }, cancellationToken: ct));

        return roles.ToList();
    }

    // En AuthRepository.cs
    public async Task<EmpresaInfoDto> GetInfoEmpresaAsync(int idEmpresa, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();

        const string sql = @"
        SELECT 
            NombreEmpresa,
            RncCedula,
            Ambiente
        FROM empresas_contratadas
        WHERE IdEmpresa = @IdEmpresa AND Estado = 1";

        return await conn.QueryFirstOrDefaultAsync<EmpresaInfoDto>(
            new CommandDefinition(sql, new { IdEmpresa = idEmpresa }, cancellationToken: ct))
            ?? throw new InvalidOperationException($"Empresa {idEmpresa} no encontrada o inactiva.");
    }

    // DTO auxiliar (puede ir en Application/DTOs/Auth/ o dentro del repositorio)
    public class EmpresaInfoDto
    {
        public string NombreEmpresa { get; set; } = string.Empty;
        public string RncCedula { get; set; } = string.Empty;
        public byte Ambiente { get; set; } = 1;
    }
    /// <summary>
    /// Obtiene el IdCliente de EmmaSystemCentral al que pertenece una empresa
    /// </summary>
    public async Task<int> GetClienteIdByEmpresaAsync(int idEmpresa, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();

        const string sql = @"
        SELECT IdCliente 
        FROM empresas_contratadas 
        WHERE IdEmpresa = @Id AND Estado = 1";

        var result = await conn.ExecuteScalarAsync<int?>(
            new CommandDefinition(sql, new { Id = idEmpresa }, cancellationToken: ct));

        // ⚠️ LOG TEMPORAL
        Console.WriteLine($"[DEBUG] GetClienteIdByEmpresa: idEmpresa={idEmpresa}, result={result}");

        return result ?? throw new InvalidOperationException(
            $"No se encontró ClienteId para Empresa {idEmpresa}");
    }
    /// <summary>
    /// Actualiza la clave secreta del cliente para validación offline
    /// </summary>
    public async Task UpdateSecretKeyAsync(int idCliente, byte[] secretKey, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        await conn.OpenAsync(ct);

        const string sql = "UPDATE clientes SET SecretKey = @Key WHERE IdCliente = @Id";
        await conn.ExecuteAsync(sql, new { Key = secretKey, Id = idCliente });
    }
    public async Task<byte[]?> GetSecretKeyAsync(int idCliente, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        await conn.OpenAsync(ct);

        const string sql = "SELECT SecretKey FROM clientes WHERE IdCliente = @IdCliente AND Estado = 1";
        return await conn.QueryFirstOrDefaultAsync<byte[]?>(sql, new { IdCliente = idCliente });
    }

    public async Task<UsuarioCentralRow?> GetUsuarioCentralByClienteIdAsync(int idCliente, CancellationToken ct)
    {
        using var conn = _centralFactory.CreateConnection();
        await conn.OpenAsync(ct);

        const string sql = @"
    SELECT TOP 1
        IdUsuarioCentral,
        IdCliente,
        Email,
        PasswordHash,
        NombreCompleto,
        EsSuperAdmin,
        Estado
    FROM usuarios_central
    WHERE IdCliente = @IdCliente AND Estado = 1
    ORDER BY IdUsuarioCentral";

        return await conn.QueryFirstOrDefaultAsync<UsuarioCentralRow>(
            new CommandDefinition(sql, new { IdCliente = idCliente }, cancellationToken: ct));
    }
}