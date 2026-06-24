namespace EmmaSystem.Application.DTOs.Ecf;

public class EcfResultadoDto
{
    public long IdEcf { get; set; }
    public string XmlCompleto { get; set; } = string.Empty;
    public string? XmlResumen { get; set; }
    public string CodigoSeguridad { get; set; } = string.Empty;
    public DateTime FechaFirma { get; set; }
}

public class EcfEnvioResultadoDto
{
    public long IdEcf { get; set; }
    public string TrackId { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? Mensaje { get; set; }
    public string XmlFirmado { get; set; } = string.Empty;
}

public class EcfEstadoDto
{
    public long IdEcf { get; set; }
    public string TipoComprobante { get; set; } = string.Empty;
    public string Secuencia { get; set; } = string.Empty;
    public string? NcfModificado { get; set; }
    public string? RncReceptor { get; set; }
    public decimal MontoTotal { get; set; }
    public DateTime FechaComprobante { get; set; }
    public string EstadoEcf { get; set; } = string.Empty;
    public string? TrackId { get; set; }
    public string? CodigoRespuesta { get; set; }
    public string? MensajeError { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public bool TieneResumen { get; set; }
}

public class ConfCertificadoDto
{
    public int Ambiente { get; set; } // 1=testecf, 2=certecf, 3=ecf
    public string AmbienteNombre { get; set; } = string.Empty;
    public bool TieneCertificado { get; set; }
    public bool TieneClave { get; set; }
    public string? Email { get; set; }
    public bool TieneClaveEmail { get; set; }
    public DateTime? FechaExpira { get; set; }
}