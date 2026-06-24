using EmmaSystem.Application.DTOs.Ecf;
using EmmaSystem.Application.Interfaces;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace EmmaSystem.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════
// INTERFAZ - Solo firmas de métodos (SIN implementación)
// ═══════════════════════════════════════════════════════════════
public interface IFacturacionElectronicaService
{
    Task<(byte[] Certificado, string Clave, int Ambiente, string Email, string ClaveEmail)> ObtenerConfiguracionAsync();
    string GenerarXmlEnMemoria(DatosFacturaElectronicaDto datos);
    string FirmarXml(string xmlSinFirmar, byte[] certificadoBytes, string passCert, out string codigoSeguridad, out DateTime fechaFirma);
    Task<string> ObtenerTokenDGIIAsync(int ambiente);
    Task<EcfEnvioResultadoDto> FirmarYEnviarAsync(
        DatosFacturaElectronicaDto datos, byte[] certificadoBytes, string passCert,
        string token, int ambiente, long? idDocumentoOrigen = null);
    Task<RespuestaConsultaDgiiDto> ConsultarEstadoDGIIAsync(string trackId, string token, int ambiente);

    string GenerarXmlResumenConsumo(DatosFacturaElectronicaDto datos); 
    bool PuedeEditarComprobante(string estadoEcf);
}

// ═══════════════════════════════════════════════════════════════
// DTO de respuesta
// ═══════════════════════════════════════════════════════════════
public class RespuestaConsultaDgiiDto
{
    public string? TrackId { get; set; }
    public string? Estado { get; set; }
    public string? Codigo { get; set; }
    public string? Mensaje { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// CLASE - Implementación completa
// ═══════════════════════════════════════════════════════════════
public class FacturacionElectronicaService : IFacturacionElectronicaService
{
    private readonly IEcfXmlRepository _ecfRepo;
    private static readonly HttpClient _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(60) };
    private const string ClaveCifrado = "Diosesbueno@7";

    public FacturacionElectronicaService(IEcfXmlRepository ecfRepo)
    {
        _ecfRepo = ecfRepo ?? throw new ArgumentNullException(nameof(ecfRepo));
    }

    public async Task<(byte[] Certificado, string Clave, int Ambiente, string Email, string ClaveEmail)> ObtenerConfiguracionAsync()
    {
        var config = await _ecfRepo.ObtenerConfiguracionCertificadoAsync();
        if (config == null)
            throw new InvalidOperationException("No existe configuración de facturación electrónica.");

        var (certBytes, clave) = await _ecfRepo.ObtenerCertificadoDigitalAsync();
        if (certBytes == null || certBytes.Length == 0)
            throw new InvalidOperationException("No se pudo obtener el certificado digital.");

        return (certBytes, clave!, config.Ambiente, config.Email ?? "", "");
    }

    public string GenerarXmlEnMemoria(DatosFacturaElectronicaDto d)
    {
        if (d == null) throw new ArgumentNullException(nameof(d));
        if (string.IsNullOrWhiteSpace(d.ENCF)) throw new ArgumentException("El eNCF es obligatorio.");
        if (string.IsNullOrWhiteSpace(d.RNCEmisor)) throw new ArgumentException("El RNC del emisor es obligatorio.");
        if (d.Items == null || !d.Items.Any()) throw new ArgumentException("Debe proporcionar al menos un ítem.");

        return ConstruirXml(d);
    }

    private string ConstruirXmlResumen(DatosFacturaElectronicaDto d)
    {
        var rfce = new XElement("RFCE");
        var encabezado = new XElement("Encabezado");
        encabezado.Add(new XElement("Version", "1.0"));

        // IdDoc - SOLO los 4 elementos del desktop
        var idDoc = new XElement("IdDoc",
            new XElement("TipoeCF", d.TipoComprobante ?? "32"),
            new XElement("eNCF", d.ENCF),
            new XElement("TipoIngresos", d.TipoIngresos ?? "01"),
            new XElement("TipoPago", d.TipoPago ?? "1")
        );

        // TablaFormasPago (obligatorio)
        var tabla = new XElement("TablaFormasPago");
        if (d.FormasPago?.Any() == true)
        {
            foreach (var fp in d.FormasPago.Where(x => x.MontoPago > 0))
            {
                tabla.Add(new XElement("FormaDePago",
                    new XElement("FormaPago", fp.FormaPago),
                    new XElement("MontoPago", fp.MontoPago.ToString("0.00", CultureInfo.InvariantCulture))
                ));
            }
        }
        else
        {
            tabla.Add(new XElement("FormaDePago",
                new XElement("FormaPago", "1"),
                new XElement("MontoPago", (d.ValorPagar > 0 ? d.ValorPagar : d.MontoTotal).ToString("0.00", CultureInfo.InvariantCulture))
            ));
        }
        idDoc.Add(tabla);
        encabezado.Add(idDoc);

        // Emisor
        var emisor = new XElement("Emisor");
        AddIfNotEmpty(emisor, "RNCEmisor", d.RNCEmisor);
        AddIfNotEmpty(emisor, "RazonSocialEmisor", d.RazonSocialEmisor);
        AddIfNotEmpty(emisor, "FechaEmision", d.FechaEmision);
        encabezado.Add(emisor);

        // Comprador
        var comprador = new XElement("Comprador");
        //AddIfNotEmpty(comprador, "RNCComprador", d.RNCComprador);
        //AddIfNotEmpty(comprador, "IdentificadorExtranjero", d.IdentificadorExtranjero);
        AddIfNotEmpty(comprador, "RazonSocialComprador", d.RazonSocialComprador);
        encabezado.Add(comprador);

        // Totales
        var totales = new XElement("Totales");
        AddIfHasValue(totales, "MontoGravadoTotal", d.MontoGravadoTotal);
        AddIfHasValue(totales, "MontoGravadoI1", d.MontoGravadoI1);
        AddIfHasValue(totales, "MontoGravadoI2", d.MontoGravadoI2);
        AddIfHasValue(totales, "MontoGravadoI3", d.MontoGravadoI3);
        AddIfHasValue(totales, "MontoExento", d.MontoExento);
        AddIfHasValue(totales, "TotalITBIS", d.TotalITBIS);
        AddIfHasValue(totales, "TotalITBIS1", d.TotalITBIS1);
        AddIfHasValue(totales, "TotalITBIS2", d.TotalITBIS2);
        AddIfHasValue(totales, "TotalITBIS3", d.TotalITBIS3);
        totales.Add(new XElement("MontoTotal", d.MontoTotal.ToString("0.00", CultureInfo.InvariantCulture)));
        encabezado.Add(totales);

        // CodigoSeguridadeCF - DEL XML COMPLETO
        encabezado.Add(new XElement("CodigoSeguridadeCF", d.CodigoSeguridad ?? ""));

        rfce.Add(encabezado);

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), rfce);

        // ✅ CRÍTICO: Formatting.None - COMPACTO SIN ESPACIOS
        using var ms = new MemoryStream();
        using var writer = new XmlTextWriter(ms, new UTF8Encoding(false)) { Formatting = Formatting.None };
        doc.Save(writer);
        writer.Flush();
        return new UTF8Encoding(false).GetString(ms.ToArray());
    }

    private static void AddIfNotEmpty(XElement parent, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            parent.Add(new XElement(name, value));
    }

    private static void AddIfHasValue(XElement parent, string name, decimal? value)
    {
        if (value.HasValue)
            parent.Add(new XElement(name, value.Value.ToString("0.00", CultureInfo.InvariantCulture)));
    }

    public string FirmarXml(string xmlSinFirmar, byte[] certificadoBytes, string passCert,
     out string codigoSeguridad, out DateTime fechaFirma)
    {
        if (string.IsNullOrWhiteSpace(xmlSinFirmar)) throw new ArgumentException("XML vacío.");
        if (certificadoBytes == null || certificadoBytes.Length == 0) throw new ArgumentException("Certificado vacío.");
        if (string.IsNullOrWhiteSpace(passCert)) throw new ArgumentException("Contraseña del certificado vacía.");

        var xmlDoc = new XmlDocument { PreserveWhitespace = true };  // ✅ CRÍTICO
        xmlDoc.LoadXml(xmlSinFirmar);

        var cert = new X509Certificate2(certificadoBytes, passCert, X509KeyStorageFlags.Exportable);
        if (cert.PrivateKey == null) throw new InvalidOperationException("El certificado no contiene clave privada.");

        using RSA privateKey = cert.GetRSAPrivateKey() ?? throw new InvalidOperationException("No se pudo obtener la clave privada RSA.");

        var signedXml = new SignedXml(xmlDoc) { SigningKey = privateKey };
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

        var reference = new Reference { Uri = "", DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256" };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(reference);

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;

        signedXml.ComputeSignature();

        byte[] signatureValueBytes = signedXml.SignatureValue;
        if (signatureValueBytes == null || signatureValueBytes.Length == 0)
            throw new InvalidOperationException("No se pudo obtener el valor de la firma.");

        string signatureValueBase64 = Convert.ToBase64String(signatureValueBytes);
        codigoSeguridad = signatureValueBase64.Substring(0, Math.Min(6, signatureValueBase64.Length));
        fechaFirma = DateTime.Now;

        XmlElement xmlFirmaDigital = signedXml.GetXml();
        xmlDoc.DocumentElement!.AppendChild(xmlDoc.ImportNode(xmlFirmaDigital, true));

        return xmlDoc.OuterXml;  // ✅ OuterXml no agrega espacios
    }

    public async Task<string> ObtenerTokenDGIIAsync(int ambiente)
    {
        throw new NotImplementedException(
            "Implementar flujo de semilla: GET /Autenticacion/api/Autenticacion/Semilla → firmar → POST /validarsemilla → obtener token");
    }

    public async Task<EcfEnvioResultadoDto> FirmarYEnviarAsync(
     DatosFacturaElectronicaDto datos, byte[] certificadoBytes, string passCert,
     string token, int ambiente, long? idDocumentoOrigen = null)
    {
        try
        {
            // ✅ PASO 1: Establecer FechaHoraFirma ANTES de generar el XML
            datos.FechaHoraFirma = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            // ✅ PASO 2: Generar XML COMPLETO (con FechaHoraFirma incluido)
            string xmlCompletoSinFirmar = GenerarXmlEnMemoria(datos);
            Console.WriteLine($"[ECF] XML completo generado para {datos.ENCF}");

            // ✅ PASO 3: Firmar XML COMPLETO
            string xmlCompletoFirmado = FirmarXml(xmlCompletoSinFirmar, certificadoBytes, passCert,
                out string codigoSeguridadCompleto, out DateTime fechaFirma);
            Console.WriteLine($"[ECF] XML completo firmado. Código seguridad: {codigoSeguridadCompleto}");

            // ✅ PASO 4: Determinar si es consumo ≤ 250k
            bool esConsumoMenor250k = (datos.TipoComprobante == "32" && datos.MontoTotal <= 250000);
            string? xmlResumenFirmado = null;

            // ✅ PASO 5: Si es consumo ≤ 250k, generar XML RESUMEN
            if (esConsumoMenor250k)
            {
                // ✅ Usar el codigoSeguridad del XML COMPLETO para el RESUMEN
                datos.CodigoSeguridad = codigoSeguridadCompleto;

                string xmlResumenSinFirmar = GenerarXmlResumenConsumo(datos);
                Console.WriteLine($"[ECF] XML resumen generado para {datos.ENCF}");

                xmlResumenFirmado = FirmarXml(xmlResumenSinFirmar, certificadoBytes, passCert,
                    out string codigoSeguridadResumen, out DateTime fechaFirmaResumen);
                Console.WriteLine($"[ECF] XML resumen firmado. Código seguridad: {codigoSeguridadResumen}");
            }

            // ✅ PASO 6: Insertar en ecf_xml
            long idEcf = await _ecfRepo.InsertarComprobanteAsync(
                datos.TipoComprobante, datos.ENCF, datos.RNCComprador,
                datos.MontoTotal, DateTime.ParseExact(datos.FechaEmision, "dd-MM-yyyy", CultureInfo.InvariantCulture),
                xmlCompletoFirmado, xmlResumenFirmado,
                codigoSeguridadCompleto, fechaFirma);
            Console.WriteLine($"[ECF] Insertado en BD con ID: {idEcf}");

            // ✅ PASO 7: Determinar qué XML enviar a DGII
            string xmlParaEnviar = esConsumoMenor250k ? xmlResumenFirmado! : xmlCompletoFirmado;
            Console.WriteLine($"[ECF] XML a enviar: {(esConsumoMenor250k ? "RESUMEN (RFCE)" : "COMPLETO (ECF)")}");

            // ✅ PASO 8: Determinar endpoint
            string ambienteNombre = ambiente switch
            {
                1 => "certecf",
                2 => "ecf",
                _ => "testecf"
            };

            string url;
            if (esConsumoMenor250k)
            {
                //$"https://fc.dgii.gov.do/{this.Ambiente}/recepcionfc/api/recepcion/ecf";
                url = $"https://fc.dgii.gov.do/{ambienteNombre}/recepcionfc/api/recepcion/ecf";
                Console.WriteLine($"[ECF] Enviando a endpoint de Consumo (≤250k): {url}");
            }
            else
            {
                url = $"https://ecf.dgii.gov.do/{ambienteNombre}/recepcion/api/facturaselectronicas";
                Console.WriteLine($"[ECF] Enviando a endpoint general: {url}");
            }

            // ✅ PASO 9: Enviar a DGII
            string trackId = "";
            string estado = "PENDIENTE";
            string? codigoRespuesta = null;
            string? mensajeError = null;

            try
            {
                var handler = new HttpClientHandler
                {
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                                   System.Security.Authentication.SslProtocols.Tls13,
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true,
                    CheckCertificateRevocationList = false,
                    UseProxy = false
                };

                using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(90) };
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Add("User-Agent", "EmmaSystem/1.0");

                byte[] xmlBytes = Encoding.UTF8.GetBytes(xmlParaEnviar);
                var fileContent = new ByteArrayContent(xmlBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/xml");

                var content = new MultipartFormDataContent();
                content.Add(fileContent, "xml", $"{datos.RNCEmisor}{datos.ENCF}.xml");
                request.Content = content;  // ← AGREGAR ESTA LÍNEA

                Console.WriteLine($"[ECF] Enviando request con {xmlBytes.Length} bytes...");
                var response = await client.SendAsync(request);
                string responseData = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[ECF] Respuesta DGII - Status: {(int)response.StatusCode} {response.StatusCode}");
                Console.WriteLine($"[ECF] Respuesta DGII - Body: {responseData}");

                if (response.IsSuccessStatusCode)
                {
                    if (esConsumoMenor250k)
                    {
                        var json = JsonDocument.Parse(responseData);
                        var root = json.RootElement;
                        estado = root.TryGetProperty("estado", out var est) ? est.GetString()?.ToUpper() ?? "ACEPTADO" : "ACEPTADO";

                        // ✅ CORREGIR: codigo puede ser número o string
                        codigoRespuesta = root.TryGetProperty("codigo", out var cod) ? cod.GetRawText().Trim('"') : null;

                        // ✅ CORREGIR: verificar que mensajes no sea null
                        if (root.TryGetProperty("mensajes", out var mensajes) && mensajes.ValueKind == JsonValueKind.Array && mensajes.GetArrayLength() > 0)
                        {
                            var mensajesList = new List<string>();
                            foreach (var msg in mensajes.EnumerateArray())
                            {
                                var codigo = msg.TryGetProperty("codigo", out var c) ? c.GetRawText().Trim('"') : "";
                                var valor = msg.TryGetProperty("valor", out var v) ? v.GetRawText().Trim('"') : "";
                                mensajesList.Add($"[{codigo}] {valor}");
                            }
                            mensajeError = string.Join("\n", mensajesList);
                        }
                        trackId = "";
                    }
                    else
                    {
                        var json = JsonDocument.Parse(responseData);
                        var root = json.RootElement;
                        trackId = root.TryGetProperty("trackId", out var track) ? track.GetString() ?? "" : "";
                        estado = "EN PROCESO";
                        Console.WriteLine($"[ECF] ✅ TrackId obtenido: {trackId}");
                    }
                }
                else
                {
                    estado = "RECHAZADO";
                    mensajeError = $"HTTP {(int)response.StatusCode}: {responseData}";
                    Console.WriteLine($"[ECF] ❌ RECHAZADO: {mensajeError}");
                }
            }
            catch (Exception ex)
            {
                estado = "ERROR";
                mensajeError = $"Error inesperado: {ex.GetType().Name}: {ex.Message} | Inner: {ex.InnerException?.Message}";
                Console.WriteLine($"[ECF] 💥 ERROR: {ex.GetType().Name}: {ex.Message}");
            }

            // ✅ PASO 10: Actualizar estado en ecf_xml
            await _ecfRepo.ActualizarEstadoAsync(idEcf, estado, codigoRespuesta, mensajeError, trackId);

            return new EcfEnvioResultadoDto
            {
                IdEcf = idEcf,
                TrackId = trackId,
                Estado = estado,
                Codigo = codigoRespuesta,
                Mensaje = mensajeError,
                XmlFirmado = xmlCompletoFirmado
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ECF] 💥 EXCEPCIÓN FATAL: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[ECF] 💥 STACK: {ex.StackTrace}");
            throw;
        }
    }

    // ✅ NUEVO MÉTODO: Calcular totales correctos
    private void CalcularTotalesCorrectos(DatosFacturaElectronicaDto datos)
    {
        // Calcular MontoGravadoI1 (18%)
        if (datos.Items != null && datos.Items.Any())
        {
            decimal montoGravado18 = datos.Items
                .Where(i => i.IndicadorFacturacion == 1)
                .Sum(i => i.MontoItem);

            decimal montoGravado16 = datos.Items
                .Where(i => i.IndicadorFacturacion == 2)
                .Sum(i => i.MontoItem);

            decimal montoExento = datos.Items
                .Where(i => i.IndicadorFacturacion == 4)
                .Sum(i => i.MontoItem);

            datos.MontoGravadoI1 = montoGravado18;
            datos.MontoGravadoI2 = montoGravado16 > 0 ? montoGravado16 : null;
            datos.MontoExento = montoExento > 0 ? montoExento : null;

            // Calcular ITBIS
            decimal totalITBIS1 = montoGravado18 * 0.18m;
            decimal totalITBIS2 = montoGravado16 * 0.16m;

            datos.TotalITBIS1 = totalITBIS1;
            datos.TotalITBIS2 = totalITBIS2 > 0 ? totalITBIS2 : null;
            datos.TotalITBIS = totalITBIS1 + totalITBIS2;

            // Calcular totales
            datos.MontoGravadoTotal = montoGravado18 + montoGravado16;
            datos.MontoTotal = datos.MontoGravadoTotal.Value + datos.TotalITBIS.Value + (datos.MontoExento ?? 0);
            datos.ValorPagar = datos.MontoTotal;
        }

        // ✅ Establecer valores por defecto para elementos obligatorios
        if (string.IsNullOrWhiteSpace(datos.IndicadorEnvioDiferido))
            datos.IndicadorEnvioDiferido = "1";

        if (string.IsNullOrWhiteSpace(datos.IndicadorServicioTodoIncluido))
            datos.IndicadorServicioTodoIncluido = "1";

        if (string.IsNullOrWhiteSpace(datos.TipoIngresos))
            datos.TipoIngresos = "01";
    }

    // ✅ NUEVO MÉTODO: Insertar datos de firma en el XML
    private string InsertarDatosFirmaEnXml(string xmlFirmado, string codigoSeguridad, DateTime fechaFirma)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlFirmado);

        // Remover Signature existente
        var signatureNode = xmlDoc.GetElementsByTagName("Signature")[0];
        if (signatureNode != null)
        {
            signatureNode.ParentNode?.RemoveChild(signatureNode);
        }

        // Insertar CodigoSeguridadeCF antes del cierre de Encabezado
        var encabezadoNode = xmlDoc.GetElementsByTagName("Encabezado")[0];
        if (encabezadoNode != null)
        {
            var codigoNode = xmlDoc.CreateElement("CodigoSeguridadeCF");
            codigoNode.InnerText = codigoSeguridad;
            encabezadoNode.AppendChild(codigoNode);
        }

        // Insertar FechaHoraFirma antes del cierre de ECF
        var ecfNode = xmlDoc.GetElementsByTagName("ECF")[0];
        if (ecfNode != null)
        {
            // Remover FechaHoraFirma existente si está vacío
            var fechaNodes = xmlDoc.GetElementsByTagName("FechaHoraFirma");
            foreach (XmlNode node in fechaNodes)
            {
                if (string.IsNullOrWhiteSpace(node.InnerText))
                {
                    node.ParentNode?.RemoveChild(node);
                }
            }

            // Agregar FechaHoraFirma con valor correcto
            var fechaNode = xmlDoc.CreateElement("FechaHoraFirma");
            fechaNode.InnerText = fechaFirma.ToString("dd-MM-yyyy HH:mm:ss");
            ecfNode.AppendChild(fechaNode);
        }

        return xmlDoc.OuterXml;
    }

    public async Task<RespuestaConsultaDgiiDto> ConsultarEstadoDGIIAsync(
        string trackId, string token, int ambiente)
    {
        if (string.IsNullOrWhiteSpace(trackId))
            throw new ArgumentException("TrackId es requerido.");

        string ambienteNombre = ambiente switch
        {
            1 => "testecf",
            2 => "certecf",
            3 => "ecf",
            _ => "testecf"
        };

        string url = $"https://ecf.dgii.gov.do/{ambienteNombre}/consultaresultado/api/consultas/estado?trackid={trackId}";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(url);
        string responseData = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Error DGII: {(int)response.StatusCode} - {responseData}");

        var json = JsonDocument.Parse(responseData);
        var root = json.RootElement;

        string? mensajeCompleto = null;
        if (root.TryGetProperty("mensajes", out var mensajes) && mensajes.GetArrayLength() > 0)
        {
            var primerMensaje = mensajes[0];
            var codigo = primerMensaje.GetProperty("Codigo").GetString();
            var valor = primerMensaje.GetProperty("valor").GetString();
            mensajeCompleto = $"{codigo} {valor}";
        }

        return new RespuestaConsultaDgiiDto
        {
            TrackId = root.GetProperty("trackId").GetString(),
            Estado = root.GetProperty("estado").GetString(),
            Codigo = root.TryGetProperty("codigo", out var cod) ? cod.GetString() : null,
            Mensaje = mensajeCompleto
        };
    }

    public bool PuedeEditarComprobante(string estadoEcf)
    {
        if (string.IsNullOrEmpty(estadoEcf)) return true;
        string estado = estadoEcf.ToUpper();
        return estado != "ACEPTADO" && estado != "ACEPTADO CONDICIONAL" && estado != "EN PROCESO";
    }
    public string GenerarXmlResumenConsumo(DatosFacturaElectronicaDto d)
    {
        if (d == null) throw new ArgumentNullException(nameof(d));
        if (string.IsNullOrWhiteSpace(d.ENCF)) throw new ArgumentException("El eNCF es obligatorio.");
        if (string.IsNullOrWhiteSpace(d.RNCEmisor)) throw new ArgumentException("El RNC del emisor es obligatorio.");

        return ConstruirXmlResumen(d);
    }

    private string ConstruirXml(DatosFacturaElectronicaDto d)
    {
        var ecf = new XElement("ECF");
        var encabezado = new XElement("Encabezado");
        encabezado.Add(new XElement("Version", "1.0"));

        // IdDoc
        var idDoc = new XElement("IdDoc");
        idDoc.Add(new XElement("TipoeCF", d.TipoComprobante));
        idDoc.Add(new XElement("eNCF", d.ENCF));
        AddIfNotEmpty(idDoc, "FechaVencimientoSecuencia", d.FechaVencimientoSecuencia);
        AddIfNotEmpty(idDoc, "IndicadorNotaCredito", d.IndicadorNotaCredito);
        AddIfNotEmpty(idDoc, "IndicadorEnvioDiferido", d.IndicadorEnvioDiferido);
        AddIfNotEmpty(idDoc, "IndicadorMontoGravado", d.IndicadorMontoGravado);
        AddIfNotEmpty(idDoc, "IndicadorServicioTodoIncluido", d.IndicadorServicioTodoIncluido);
        AddIfNotEmpty(idDoc, "TipoIngresos", d.TipoIngresos);
        AddIfNotEmpty(idDoc, "TipoPago", d.TipoPago);
        AddIfNotEmpty(idDoc, "FechaLimitePago", d.FechaLimitePago);
        AddIfNotEmpty(idDoc, "TerminoPago", d.TerminoPago);

        if (d.FormasPago?.Any() == true)
        {
            var tabla = new XElement("TablaFormasPago");
            foreach (var fp in d.FormasPago.Where(x => !string.IsNullOrWhiteSpace(x.FormaPago)))
            {
                tabla.Add(new XElement("FormaDePago",
                    new XElement("FormaPago", fp.FormaPago),
                    new XElement("MontoPago", fp.MontoPago.ToString("0.00", CultureInfo.InvariantCulture))));
            }
            if (tabla.HasElements) idDoc.Add(tabla);
        }
        else
        {
            var tabla = new XElement("TablaFormasPago");
            tabla.Add(new XElement("FormaDePago",
                new XElement("FormaPago", "1"),
                new XElement("MontoPago", d.ValorPagar.ToString("0.00", CultureInfo.InvariantCulture))));
            idDoc.Add(tabla);
        }

        encabezado.Add(idDoc);

        // Emisor
        var emisor = new XElement("Emisor");
        AddIfNotEmpty(emisor, "RNCEmisor", d.RNCEmisor);
        AddIfNotEmpty(emisor, "RazonSocialEmisor", d.RazonSocialEmisor);
        AddIfNotEmpty(emisor, "NombreComercial", d.NombreComercial);
        AddIfNotEmpty(emisor, "DireccionEmisor", d.DireccionEmisor);
        AddIfNotEmpty(emisor, "Municipio", d.Municipio);
        AddIfNotEmpty(emisor, "Provincia", d.Provincia);

        if (d.TelefonosEmisor?.Any() == true)
        {
            var tel = new XElement("TablaTelefonoEmisor");
            foreach (var t in d.TelefonosEmisor.Where(x => !string.IsNullOrWhiteSpace(x)))
                tel.Add(new XElement("TelefonoEmisor", t));
            if (tel.HasElements) emisor.Add(tel);
        }

        AddIfNotEmpty(emisor, "CorreoEmisor", d.CorreoEmisor);
        AddIfNotEmpty(emisor, "WebSite", d.WebSite);
        AddIfNotEmpty(emisor, "FechaEmision", d.FechaEmision);
        encabezado.Add(emisor);

        // Comprador
        if (!string.IsNullOrWhiteSpace(d.RazonSocialComprador))
        {
            var comprador = new XElement("Comprador");
            AddIfNotEmpty(comprador, "RNCComprador", d.RNCComprador);
            AddIfNotEmpty(comprador, "IdentificadorExtranjero", d.IdentificadorExtranjero);
            AddIfNotEmpty(comprador, "RazonSocialComprador", d.RazonSocialComprador);
            AddIfNotEmpty(comprador, "CorreoComprador", d.CorreoComprador);
            AddIfNotEmpty(comprador, "DireccionComprador", d.DireccionComprador);
            AddIfNotEmpty(comprador, "TelefonoAdicional", d.TelefonoAdicional);
            encabezado.Add(comprador);
        }

        // Totales - SOLO agregar ITBIS2/TotalITBIS2 si hay valor > 0
        var totales = new XElement("Totales");
        AddIfHasValue(totales, "MontoGravadoTotal", d.MontoGravadoTotal);
        AddIfHasValue(totales, "MontoGravadoI1", d.MontoGravadoI1);
        AddIfNotEmpty(totales, "ITBIS1", d.ITBIS1 ?? "18");
        if (d.MontoGravadoI2.HasValue && d.MontoGravadoI2 > 0)
        {
            AddIfHasValue(totales, "MontoGravadoI2", d.MontoGravadoI2);
            AddIfNotEmpty(totales, "ITBIS2", d.ITBIS2 ?? "16");
        }
        if (d.MontoGravadoI3.HasValue && d.MontoGravadoI3 > 0)
        {
            AddIfHasValue(totales, "MontoGravadoI3", d.MontoGravadoI3);
            AddIfNotEmpty(totales, "ITBIS3", d.ITBIS3 ?? "0");
        }
        AddIfHasValue(totales, "MontoExento", d.MontoExento);

        AddIfHasValue(totales, "TotalITBIS", d.TotalITBIS);
        AddIfHasValue(totales, "TotalITBIS1", d.TotalITBIS1);
        if (d.TotalITBIS2.HasValue && d.TotalITBIS2 > 0)
            AddIfHasValue(totales, "TotalITBIS2", d.TotalITBIS2);
        if (d.TotalITBIS3.HasValue && d.TotalITBIS3 > 0)
            AddIfHasValue(totales, "TotalITBIS3", d.TotalITBIS3);
        totales.Add(new XElement("MontoTotal", d.MontoTotal.ToString("0.00", CultureInfo.InvariantCulture)));
        totales.Add(new XElement("ValorPagar", d.ValorPagar.ToString("0.00", CultureInfo.InvariantCulture)));
        encabezado.Add(totales);

        ecf.Add(encabezado);

        // Items - SIN DescripcionItem
        var items = new XElement("DetallesItems");
        for (int i = 0; i < d.Items.Count; i++)
        {
            var item = d.Items[i];
            var xmlItem = new XElement("Item",
                new XElement("NumeroLinea", i + 1),
                new XElement("IndicadorFacturacion", item.IndicadorFacturacion),
                new XElement("NombreItem", item.NombreItem ?? ""),
                new XElement("IndicadorBienoServicio", item.IndicadorBienoServicio),
                new XElement("CantidadItem", item.CantidadItem.ToString("0.00", CultureInfo.InvariantCulture)),
                new XElement("UnidadMedida", item.UnidadMedida),
                new XElement("PrecioUnitarioItem", item.PrecioUnitarioItem.ToString("0.00", CultureInfo.InvariantCulture)),
                new XElement("MontoItem", item.MontoItem.ToString("0.00", CultureInfo.InvariantCulture))
            );
            items.Add(xmlItem);
        }
        ecf.Add(items);

        // FechaHoraFirma - SOLO si tiene valor
        if (!string.IsNullOrWhiteSpace(d.FechaHoraFirma))
        {
            ecf.Add(new XElement("FechaHoraFirma", d.FechaHoraFirma));
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), ecf);

        // ✅ CRÍTICO: Formatting.None - COMPACTO SIN ESPACIOS
        using var ms = new MemoryStream();
        using var xw = new XmlTextWriter(ms, new UTF8Encoding(false)) { Formatting = Formatting.None };
        doc.Save(xw);
        xw.Flush();
        return new UTF8Encoding(false).GetString(ms.ToArray());
    }
}