using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Settings;

namespace EmmaSystem.Infrastructure.Data;

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

    public async Task<SqlConnection> CrearConexionAsync(int empresaId)
    {
        if (empresaId <= 0)
            throw new ArgumentException("El ID de empresa debe ser mayor a 0", nameof(empresaId));

        // ✅ Cachear la CADENA DE CONEXIÓN, no la conexión
        var cacheKey = $"connstr_{empresaId}";

        string cadenaConexion;

        if (!_cache.TryGetValue(cacheKey, out cadenaConexion!))
        {
            // Obtener datos de la empresa desde EmmaSystemCentral
            var empresa = await ObtenerEmpresaAsync(empresaId);

            if (empresa.Estado != 1)
            {
                throw new InvalidOperationException(
                    $"La empresa '{empresa.NombreEmpresa}' no está activa. Estado actual: {ObtenerNombreEstado(empresa.Estado)}");
            }

            // Descifrar la cadena de conexión
            cadenaConexion = _cifradoService.Descifrar(
                empresa.CadenaConexionEnc,
                empresa.SaltCifrado,
                empresa.VectorIV);

            // ✅ Cachear solo la cadena de conexión (no la conexión abierta)
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(_settings.MinutosCache));

            _cache.Set(cacheKey, cadenaConexion, cacheOptions);
        }

        // ✅ Crear una NUEVA conexión cada vez (práctica recomendada)
        var connection = new SqlConnection(cadenaConexion);
        await connection.OpenAsync();

        return connection;
    }

    private async Task<EmpresaContratada> ObtenerEmpresaAsync(int empresaId)
    {
        using var connectionCentral = new SqlConnection(_settings.CadenaConexionCentral);
        await connectionCentral.OpenAsync();

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

        var row = await connectionCentral.QueryFirstOrDefaultAsync(sql, new { EmpresaId = empresaId });

        if (row is null)
            throw new InvalidOperationException($"No se encontró la empresa con ID {empresaId}");

        return new EmpresaContratada
        {
            IdEmpresa = (int)row.IdEmpresa,
            NombreEmpresa = (string)row.NombreEmpresa,
            CadenaConexionEnc = (byte[])row.CadenaConexionEnc,
            VectorIV = (byte[])row.VectorIV,
            Estado = (int)row.Estado,
            SaltCifrado = (byte[])row.SaltCifrado
        };
    }

    private static string ObtenerNombreEstado(int estado) => estado switch
    {
        1 => "Activo",
        2 => "Suspendido",
        3 => "Cancelado",
        _ => "Desconocido"
    };

    private class EmpresaContratada
    {
        public int IdEmpresa { get; set; }
        public string NombreEmpresa { get; set; } = string.Empty;
        public byte[] CadenaConexionEnc { get; set; } = Array.Empty<byte>();
        public byte[] VectorIV { get; set; } = Array.Empty<byte>();
        public int Estado { get; set; }
        public byte[] SaltCifrado { get; set; } = Array.Empty<byte>();
    }
}