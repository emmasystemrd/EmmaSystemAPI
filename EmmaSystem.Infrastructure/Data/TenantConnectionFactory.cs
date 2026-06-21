using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Settings;

namespace EmmaSystem.Infrastructure.Data;

/// <summary>
/// Factory para crear conexiones dinámicas a bases de datos de empresas
/// </summary>
public class TenantConnectionFactory : ITenantConnectionFactory
{
    private readonly EmmaSystemSettings _settings;
    private readonly ICifradoService _cifradoService;
    private readonly IMemoryCache _cache;

    public TenantConnectionFactory(
        EmmaSystemSettings settings,
        ICifradoService cifradoService,
        IMemoryCache cache)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _cifradoService = cifradoService ?? throw new ArgumentNullException(nameof(cifradoService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc/>
    public async Task<SqlConnection> CrearConexionAsync(int empresaId)
    {
        if (empresaId <= 0)
            throw new ArgumentException("El ID de empresa debe ser mayor a 0", nameof(empresaId));

        var cacheKey = $"conn_{empresaId}";

        // Intentar obtener de cache
        if (_cache.TryGetValue(cacheKey, out SqlConnection? cachedConnection))
        {
            if (cachedConnection is not null && cachedConnection.State == System.Data.ConnectionState.Open)
            {
                return cachedConnection;
            }
        }

        // Obtener datos de la empresa desde EmmaSystemCentral
        var empresa = await ObtenerEmpresaAsync(empresaId);

        if (empresa.Estado != 1)
        {
            throw new InvalidOperationException(
                $"La empresa '{empresa.NombreEmpresa}' no está activa. Estado actual: {ObtenerNombreEstado(empresa.Estado)}");
        }

        // Descifrar la cadena de conexión
        var cadenaConexion = _cifradoService.Descifrar(
            empresa.CadenaConexionEnc,
            empresa.SaltCifrado,
            empresa.VectorIV);

        // Crear y abrir la conexión
        var connection = new SqlConnection(cadenaConexion);
        await connection.OpenAsync();

        // Cachear la conexión
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_settings.MinutosCache))
            .RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                if (value is SqlConnection conn && conn.State == System.Data.ConnectionState.Open)
                {
                    conn.Dispose();
                }
            });

        _cache.Set(cacheKey, connection, cacheOptions);

        return connection;
    }
    private async Task<EmpresaContratada> ObtenerEmpresaAsync(int empresaId)
    {
        using var connectionCentral = new SqlConnection(_settings.CadenaConexionCentral);
        await connectionCentral.OpenAsync();

        // ✅ Usar VARBINARY(MAX) explícitamente y mapeo manual
        const string sql = @"
        SELECT 
            ec.IdEmpresa,
            ec.NombreEmpresa,
            CAST(ec.CadenaConexionEnc AS VARBINARY(MAX)) AS CadenaConexionEnc,
            CAST(ec.VectorIV AS VARBINARY(16)) AS VectorIV,
            ec.Estado,
            CAST(c.SaltCifrado AS VARBINARY(64)) AS SaltCifrado
        FROM empresas_contratadas ec
        INNER JOIN clientes c ON ec.IdCliente = c.IdCliente
        WHERE ec.IdEmpresa = @EmpresaId AND ec.Estado = 1";

        // ✅ Mapeo MANUAL usando QueryFirstOrDefault<dynamic> para evitar problemas de tipo
        var row = await connectionCentral.QueryFirstOrDefaultAsync(sql, new { EmpresaId = empresaId });

        if (row is null)
            throw new InvalidOperationException($"No se encontró la empresa con ID {empresaId}");

        // Conversión explícita y segura
        var saltBytes = (byte[])row.SaltCifrado;

        // ⚠️ LOG TEMPORAL PARA DIAGNÓSTICO
        Console.WriteLine($"[DEBUG-MAPEO] Salt Length: {saltBytes.Length}");
        Console.WriteLine($"[DEBUG-MAPEO] Salt Hex: {Convert.ToHexString(saltBytes)}");
        Console.WriteLine($"[DEBUG-MAPEO] Último byte: {saltBytes[^1]:X2}");

        return new EmpresaContratada
        {
            IdEmpresa = (int)row.IdEmpresa,
            NombreEmpresa = (string)row.NombreEmpresa,
            CadenaConexionEnc = (byte[])row.CadenaConexionEnc,
            VectorIV = (byte[])row.VectorIV,
            Estado = (int)row.Estado,
            SaltCifrado = saltBytes
        };
    }
    /// <summary>
    /// Convierte el código de estado a nombre descriptivo
    /// </summary>
    private static string ObtenerNombreEstado(int estado) => estado switch
    {
        1 => "Activo",
        2 => "Suspendido",
        3 => "Cancelado",
        _ => "Desconocido"
    };

    /// <summary>
    /// DTO interno para mapear datos de empresa desde EmmaSystemCentral
    /// </summary>
    private class EmpresaContratada
    {
        public int IdEmpresa { get; set; }
        public string NombreEmpresa { get; set; } = string.Empty;
        public byte[] CadenaConexionEnc { get; set; } = Array.Empty<byte>();  // ← VARBINARY(MAX)
        public byte[] VectorIV { get; set; } = Array.Empty<byte>();           // ← VARBINARY(16)
        public int Estado { get; set; }
        public byte[] SaltCifrado { get; set; } = Array.Empty<byte>();        // ← VARBINARY(64) desde clientes
        public string RncCedula { get; set; } = string.Empty;
        public byte Ambiente { get; set; } = 1;
    }
}