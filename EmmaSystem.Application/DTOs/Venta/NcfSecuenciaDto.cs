namespace EmmaSystem.Application.DTOs.Venta;

public class NcfSecuenciaDto
{
    public string Tipo { get; set; } = string.Empty;
    public decimal Desde { get; set; }
    public decimal Hasta { get; set; }
    public decimal Secuencia { get; set; }
    public decimal Secuencia_eCF { get; set; }
    public string? Aviso { get; set; }
    public DateTime? Vencimiento { get; set; }
    public string NcfGenerado { get; set; } = string.Empty;
}