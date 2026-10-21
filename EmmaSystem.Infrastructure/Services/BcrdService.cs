using System.Net.Http.Json;
using System.Text.Json;
using EmmaSystem.Application.DTOs.BCRD;
using EmmaSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EmmaSystem.Infrastructure.Services;

public class BcrdService : IBcrdService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public BcrdService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<List<TasaCambioDto>> ObtenerTasasAsync(DateTime fechaDesde, DateTime fechaHasta)
    {
        var token = _configuration["BCRD:Token"];

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("No está configurado el token del BCRD.");

        var request = new
        {
            fromDate = fechaDesde.ToString("yyyy-MM-ddTHH:mm:ss"),
            toDate = fechaHasta.ToString("yyyy-MM-ddTHH:mm:ss"),
            token,
            skipCount = 0,
            maxResultCount = 100
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "/api/services/app/MacroVariables/HistoricoTasas", request);

        var contenido = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"BCRD HTTP {(int)response.StatusCode}: {contenido}");

        var resultado = JsonSerializer.Deserialize<BcrdResponse>(contenido,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return resultado?.Result?.Values?
            .SelectMany(x => x.Items ?? [])
            .Select(x => new TasaCambioDto
            {
                Compra = x.BuyingValue,
                Venta = x.SellingValue,
                MonedaId = x.CurrencyId,
                Fecha = x.ExchangeRateDate
            })
            .ToList() ?? [];
    }

    private class BcrdResponse
    {
        public BcrdResult? Result { get; set; }
    }

    private class BcrdResult
    {
        public List<BcrdValue>? Values { get; set; }
    }

    private class BcrdValue
    {
        public List<BcrdItem>? Items { get; set; }
    }

    private class BcrdItem
    {
        public decimal BuyingValue { get; set; }
        public decimal SellingValue { get; set; }
        public int CurrencyId { get; set; }
        public DateTime ExchangeRateDate { get; set; }
    }
}