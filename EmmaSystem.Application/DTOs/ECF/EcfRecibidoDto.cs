namespace EmmaSystem.Application.DTOs.ECF;

public class EcfRecibidoDto
{
    public long Id { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string RncEmisor { get; set; } = string.Empty;
    public string RncComprador { get; set; } = string.Empty;
    public string ENCF { get; set; } = string.Empty;
    public string? TipoECF { get; set; }
    public DateTime? FechaEmision { get; set; }
    public decimal? MontoTotal { get; set; }
    public decimal? TotalItbis { get; set; }
    public int Estado { get; set; }
    public string? CodigoMotivoRechazo { get; set; }
    public string? MotivoRechazo { get; set; }
    public string? XmlRecibido { get; set; }
    public DateTime FechaRecepcion { get; set; }
}