using EmmaSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace EmmaSystem.Infrastructure.Services;

public interface ITokenDgiiService
{
    Task<string> ObtenerTokenAsync(int ambiente, byte[] certificadoBytes, string claveCertificado);
}

public class TokenDgiiService : ITokenDgiiService
{
    private readonly IMemoryCache _cache;

    public TokenDgiiService(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<string> ObtenerTokenAsync(int ambiente, byte[] certificadoBytes, string claveCertificado)
    {
        // ✅ MAPEO CONSISTENTE CON EL DESKTOP
        // Ambiente: 0=testecf, 1=certecf, 2=ecf
        string ambienteNombre = ambiente switch
        {
            1 => "certecf",
            2 => "ecf",
            _ => "testecf"  // Default: testecf (incluye 0 y null)
        };

        string cacheKey = $"token_dgii_{ambienteNombre}";

        // Verificar token cacheado
        if (_cache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            Console.WriteLine($"[TOKEN] ✅ Token cacheado válido para {ambienteNombre}");
            return cachedToken!;
        }

        Console.WriteLine($"[TOKEN] 🔄 Obteniendo nuevo token para ambiente: {ambienteNombre}");

        // ✅ SIEMPRE USAR "CerteCF" PARA AUTENTICACIÓN (igual que el desktop)
        // El desktop usa: https://eCF.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/Semilla
        string urlSemilla = $"https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/Semilla";
        string urlValidar = $"https://ecf.dgii.gov.do/CerteCF/Autenticacion/api/Autenticacion/ValidarSemilla";

        Console.WriteLine($"[TOKEN] URL Semilla: {urlSemilla}");

        var handler = new HttpClientHandler
        {
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                           System.Security.Authentication.SslProtocols.Tls13,
            ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        };

        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        // 1. Obtener semilla
        HttpResponseMessage responseSemilla;
        string xmlSemilla;
        try
        {
            responseSemilla = await client.GetAsync(urlSemilla);
            responseSemilla.EnsureSuccessStatusCode();
            xmlSemilla = await responseSemilla.Content.ReadAsStringAsync();
            Console.WriteLine($"[TOKEN] ✅ Semilla obtenida ({xmlSemilla.Length} chars)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TOKEN] ❌ Error obteniendo semilla: {ex.Message}");
            throw new InvalidOperationException($"No se pudo obtener la semilla de DGII: {ex.Message}", ex);
        }

        // 2. Firmar semilla
        string xmlSemillaFirmada;
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlSemilla);

            var cert = new X509Certificate2(
                certificadoBytes, claveCertificado,
                X509KeyStorageFlags.Exportable);

            using var privateKey = cert.GetRSAPrivateKey()
                ?? throw new InvalidOperationException("No se pudo obtener la clave privada RSA.");

            var signedXml = new SignedXml(xmlDoc) { SigningKey = privateKey };
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

            var reference = new Reference { Uri = "", DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256" };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);

            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(cert));
            signedXml.KeyInfo = keyInfo;

            signedXml.ComputeSignature();

            XmlElement xmlFirma = signedXml.GetXml();
            xmlDoc.DocumentElement!.AppendChild(xmlDoc.ImportNode(xmlFirma, true));

            xmlSemillaFirmada = xmlDoc.OuterXml;
            Console.WriteLine($"[TOKEN] ✅ Semilla firmada ({xmlSemillaFirmada.Length} chars)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TOKEN] ❌ Error firmando semilla: {ex.Message}");
            throw new InvalidOperationException($"Error al firmar la semilla: {ex.Message}", ex);
        }

        // 3. Enviar semilla firmada
        string token;
        string expira;
        try
        {
            Console.WriteLine($"[TOKEN] URL Validar: {urlValidar}");

            byte[] xmlBytes = System.Text.Encoding.UTF8.GetBytes(xmlSemillaFirmada);

            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(xmlBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml");
            content.Add(fileContent, "xml", "semilla.xml");

            var responseValidar = await client.PostAsync(urlValidar, content);
            string jsonRespuesta = await responseValidar.Content.ReadAsStringAsync();

            Console.WriteLine($"[TOKEN] Respuesta ValidarSemilla - Status: {(int)responseValidar.StatusCode}");
            Console.WriteLine($"[TOKEN] Respuesta ValidarSemilla - Body: {jsonRespuesta}");

            if (!responseValidar.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"DGII rechazó la semilla: {(int)responseValidar.StatusCode} - {jsonRespuesta}");
            }

            var json = System.Text.Json.JsonDocument.Parse(jsonRespuesta);

            token = json.RootElement.GetProperty("token").GetString()
                ?? throw new InvalidOperationException("No se recibió token de DGII.");

            expira = json.RootElement.GetProperty("expira").GetString()
                ?? throw new InvalidOperationException("No se recibió fecha de expiración.");

            Console.WriteLine($"[TOKEN] ✅ Token obtenido. Expira: {expira}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TOKEN] ❌ Error validando semilla: {ex.Message}");
            throw new InvalidOperationException($"Error al validar la semilla con DGII: {ex.Message}", ex);
        }

        // 4. Cachear token hasta su expiración (menos 5 minutos de margen)
        try
        {
            DateTime fechaExpira = DateTime.Parse(expira);
            TimeSpan duracion = fechaExpira - DateTime.Now;
            if (duracion.TotalMinutes > 5)
            {
                duracion = duracion.Subtract(TimeSpan.FromMinutes(5));
            }
            else if (duracion.TotalMinutes <= 0)
            {
                duracion = TimeSpan.FromMinutes(5); // Mínimo 5 minutos
            }

            _cache.Set(cacheKey, token, duracion);
            Console.WriteLine($"[TOKEN] ✅ Token cacheado por {duracion.TotalMinutes:F0} minutos");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TOKEN] ⚠️ Error cacheando token: {ex.Message}. Token válido por 30 min por defecto.");
            _cache.Set(cacheKey, token, TimeSpan.FromMinutes(30));
        }

        return token;
    }
}