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

    /// <summary>
    /// Obtiene los datos de la empresa desde EmmaSystemCentral
    /// </summary>
    private async Task<EmpresaContratada> ObtenerEmpresaAsync(int empresaId)
    {
        using var connectionCentral = new SqlConnection(_settings.CadenaConexionCentral);
        await connectionCentral.OpenAsync();

        const string sql = @"
            SELECT 
                ec.IdEmpresa,
                ec.NombreEmpresa,
                ec.CadenaConexionEnc,
                ec.VectorIV,
                ec.Estado,
                c.SaltCifrado
            FROM empresas_contratadas ec
            INNER JOIN clientes c ON ec.IdCliente = c.IdCliente
            WHERE ec.IdEmpresa = @EmpresaId";

        var empresa = await connectionCentral.QueryFirstOrDefaultAsync<EmpresaContratada>(
            sql,
            new { EmpresaId = empresaId });

        if (empresa is null)
        {
            throw new InvalidOperationException(
                $"No se encontró la empresa con ID {empresaId} en EmmaSystemCentral");
        }

        return empresa;
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
        public string CadenaConexionEnc { get; set; } = string.Empty;
        public string VectorIV { get; set; } = string.Empty;
        public int Estado { get; set; }
        public string SaltCifrado { get; set; } = string.Empty;
    }
}