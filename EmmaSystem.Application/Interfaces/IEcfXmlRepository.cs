using EmmaSystem.Application.DTOs.Ecf;

namespace EmmaSystem.Application.Interfaces;

public interface IEcfXmlRepository
{
    Task<long> InsertarComprobanteAsync(
        string tipoComprobante, string secuencia, string? rncReceptor,
        decimal montoTotal, DateTime fechaComprobante,
        string xmlCompleto, string? xmlResumen,
        string codigoSeguridad, DateTime fechaFirma,
        long? idDocumentoOrigen = null, CancellationToken ct = default);

    Task ActualizarEstadoAsync(
        long idEcf, string estadoEcf, string? codigoRespuesta,
        string? mensajeError, string? trackId = null, CancellationToken ct = default);

    Task AnularAsync(long idEcf, CancellationToken ct = default);

    Task<string> ObtenerEstadoAsync(string secuencia, CancellationToken ct = default);

    Task<EcfResultadoDto?> ObtenerPorSecuenciaAsync(string secuencia, CancellationToken ct = default);

    Task<IReadOnlyList<EcfEstadoDto>> ListarAsync(
        DateTime? fechaInicio, DateTime? fechaFin,
        string? estadoEcf, string? tipoComprobante, CancellationToken ct = default);

    Task<(string? XmlCompleto, string RncEmisor, string Secuencia)> ObtenerXmlCompletoAsync(
        long idEcf, CancellationToken ct = default);

    Task<(string? XmlResumen, string RncEmisor, string Secuencia)> ObtenerXmlResumenAsync(
        long idEcf, CancellationToken ct = default);

    Task<ConfCertificadoDto?> ObtenerConfiguracionCertificadoAsync(CancellationToken ct = default);

    // Agregar a la interfaz IEcfXmlRepository
    Task<(byte[]? Certificado, string? Clave)> ObtenerCertificadoDigitalAsync(CancellationToken ct = default);
    Task<(string RNC, string Nombre, string Direccion, string Email, string Url)> ObtenerDatosEmpresaAsync(int idEmpresa, CancellationToken ct = default);
    Task<(string NumDocumento, string RazonSocial, string Email, string Direccion)> ObtenerDatosClienteAsync(int idCliente, CancellationToken ct = default);

    // Agregar esta firma a la interfaz
    Task<Dictionary<string, string>> ObtenerEstadosPorNcfAsync(IEnumerable<string> ncfs, CancellationToken ct = default);
    Task<EcfDatosImpresionDto?> ObtenerDatosEcfAsync(string ncf, CancellationToken ct = default);
}