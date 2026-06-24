namespace EmmaSystem.Application.DTOs.Ecf;

public class EcfDatosImpresionDto
{
    public string Secuencia { get; set; } = string.Empty;
    public string? CodigoSeguridad { get; set; }
    public DateTime? FechaFirma { get; set; }
    public int Ambiente { get; set; }  // 0=testecf, 1=certecf, 2=ecf
    public string? EstadoEcf { get; set; }
}