using EmmaSystem.Application.DTOs.Dgii;
using EmmaSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octetus.ConsultasDgii;
using Octetus.ConsultasDgii.Services;

namespace EmmaSystem.Application.Services;

public class DgiiService : IDgiiService
{
    private readonly ILogger<DgiiService> _logger;
    private readonly IMemoryCache _cache;

    public DgiiService(ILogger<DgiiService> logger, IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Consulta información detallada de un contribuyente por RNC/Cédula
    /// </summary>
    public async Task<ContribuyenteDto?> ConsultarRncAsync(string rncOCedula)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rncOCedula))
                return null;

            var rncLimpio = rncOCedula.Trim().Replace("-", "");
            var cacheKey = $"dgii_contribuyente_{rncLimpio}";

            // Intentar obtener del caché
            if (_cache.TryGetValue(cacheKey, out ContribuyenteDto? cached))
            {
                _logger.LogInformation("✅ Contribuyente obtenido del caché: {RNC}", rncLimpio);
                return cached;
            }

            var dgii = new ServicioConsultasWebDgii();
            var response = dgii.ConsultarRncContribuyentes(rncLimpio);

            if (!response.Success)
            {
                _logger.LogWarning("❌ Consulta Contribuyente fallida para {RNC}: {Msg}", rncLimpio, response.Message);
                return null;
            }

            var result = new ContribuyenteDto
            {
                RncCedula = response.CedulaORnc ?? string.Empty,
                Nombre = response.NombreORazónSocial ?? string.Empty,
                NombreComercial = response.NombreComercial ?? string.Empty,
                Categoria = response.Categoría ?? string.Empty,
                Estado = response.Estado ?? string.Empty,
                TipoContribuyente = rncOCedula.Length == 9 ? "Persona Jurídica" : "Persona Física",
                RegimenPagos = response.RegimenDePagos ?? string.Empty
            };

            // Guardar en caché por 24 horas
            _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
            _logger.LogInformation("✅ Contribuyente consultado y cacheado: {RNC} - {Nombre}", result.RncCedula, result.Nombre);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando Contribuyente {RNC}", rncOCedula);
            return null;
        }
    }

    /// <summary>
    /// Consulta RNC registrado en la DGII (información básica)
    /// </summary>
    public async Task<RncRegistradoDto?> ConsultarRncRegistradoAsync(string rncOCedula)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rncOCedula))
                return null;

            var rncLimpio = rncOCedula.Trim().Replace("-", "");
            var cacheKey = $"dgii_rnc_registrado_{rncLimpio}";

            if (_cache.TryGetValue(cacheKey, out RncRegistradoDto? cached))
            {
                _logger.LogInformation("✅ RNC Registrado obtenido del caché: {RNC}", rncLimpio);
                return cached;
            }

            _logger.LogInformation("🔍 Iniciando consulta RNC Registrado para: {RNC}", rncLimpio);

            var dgii = new ServicioConsultasWebDgii();

            _logger.LogInformation("📞 Llamando a ConsultarRncRegistrados...");
            var response = dgii.ConsultarRncRegistrados(rncLimpio);

            _logger.LogInformation("📥 Respuesta recibida - Success: {Success}, Message: {Message}",
                response.Success, response.Message ?? "null");

            if (!response.Success)
            {
                _logger.LogWarning("❌ Consulta fallida - RNC: {RNC}, Message: {Msg}",
                    rncLimpio, response.Message);
                return null;
            }

            _logger.LogInformation("✅ Datos extraídos - Nombre: {Nombre}, RNC: {RNC}",
                response.Nombre, response.RncOCedula);

            var result = new RncRegistradoDto
            {
                RncOCedula = response.RncOCedula ?? string.Empty,
                Nombre = response.Nombre ?? string.Empty,
                NombreComercial = response.Nombre ?? string.Empty,
                ActividadEconomica = response.Actividad ?? string.Empty,
                TipoContribuyente = response.Tipo ?? string.Empty,
                Estado = response.Estado ?? string.Empty
            };

            _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
            _logger.LogInformation("✅ RNC Registrado consultado y cacheado: {RNC} - {Nombre}",
                result.RncOCedula, result.Nombre);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ EXCEPCIÓN consultando RNC Registrado {RNC} - Tipo: {Type}",
                rncOCedula, ex.GetType().Name);
            return null;
        }
    }

    /// <summary>
    /// Valida un NCF en la DGII
    /// </summary>
    public async Task<NcfValidationDto?> ConsultarNcfAsync(string ncf, string rncEmisor)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ncf) || string.IsNullOrWhiteSpace(rncEmisor))
                return null;

            var ncfLimpio = ncf.Trim();
            var rncLimpio = rncEmisor.Trim().Replace("-", "");
            var cacheKey = $"dgii_ncf_{rncLimpio}_{ncfLimpio}";

            // Intentar obtener del caché
            if (_cache.TryGetValue(cacheKey, out NcfValidationDto? cached))
            {
                _logger.LogInformation("✅ NCF obtenido del caché: {NCF}", ncfLimpio);
                return cached;
            }

            var dgii = new ServicioConsultasWebDgii();
            var response = dgii.ConsultarNcf(ncfLimpio, rncLimpio);

            if (!response.Success)
            {
                _logger.LogWarning("❌ Consulta NCF fallida para {NCF}: {Msg}", ncfLimpio, response.Message);
                return null;
            }

            var result = new NcfValidationDto
            {
                Ncf = ncfLimpio,
                RncEmisor = rncLimpio,
                TipoComprobante = response.TipoDeComprobante ?? string.Empty,
                Estado = response.Estado ?? string.Empty,
                RncComprador = response.RncOCedula ?? string.Empty,
                NombreComprador = response.NombreORazónSocial ?? string.Empty,
                MontoTotal = 0,
                FechaEmision = string.Empty,
            };

            // Guardar en caché por 1 hora (los NCF pueden cambiar de estado)
            _cache.Set(cacheKey, result, TimeSpan.FromHours(1));
            _logger.LogInformation("✅ NCF consultado y cacheado: {NCF} - Estado: {Estado}", ncfLimpio, result.Estado);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando NCF {NCF}", ncf);
            return null;
        }
    }

    /// <summary>
    /// Valida un e-NCF en la DGII
    /// </summary>
    public async Task<ENcfValidationDto?> ConsultarENcfAsync(
        string rncEmisor, string encf, string rncComprador, string codigoSeguridad)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rncEmisor) || string.IsNullOrWhiteSpace(encf))
                return null;

            var rncEmisorLimpio = rncEmisor.Trim().Replace("-", "");
            var rncCompradorLimpio = rncComprador?.Trim().Replace("-", "") ?? string.Empty;
            var encfLimpio = encf.Trim();
            var codigoLimpio = codigoSeguridad?.Trim() ?? string.Empty;

            var cacheKey = $"dgii_encf_{rncEmisorLimpio}_{encfLimpio}";

            // Intentar obtener del caché
            if (_cache.TryGetValue(cacheKey, out ENcfValidationDto? cached))
            {
                _logger.LogInformation("✅ e-NCF obtenido del caché: {eNCF}", encfLimpio);
                return cached;
            }

            var dgii = new ServicioConsultasWebDgii();
            var response = dgii.ConsultarENcf(rncEmisorLimpio, encfLimpio, rncCompradorLimpio, codigoLimpio);

            if (!response.Success)
            {
                _logger.LogWarning("❌ Consulta e-NCF fallida para {eNCF}: {Msg}", encfLimpio, response.Message);
                return null;
            }

            var result = new ENcfValidationDto
            {
                Estado = response.Estado ?? string.Empty,
                FechaEmision = response.FechaEmision,
                MontoTotal = response.MontoTotal
            };

            // Guardar en caché por 1 hora
            _cache.Set(cacheKey, result, TimeSpan.FromHours(1));
            _logger.LogInformation("✅ e-NCF consultado y cacheado: {eNCF} - Estado: {Estado}", encfLimpio, result.Estado);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando e-NCF {eNCF}", encf);
            return null;
        }
    }
}