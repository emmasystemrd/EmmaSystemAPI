namespace EmmaSystem.Application.DTOs.Reportes;

public class VentaReporteDto
{
    public DateTime Fecha { get; set; }
    public string Condicion { get; set; } = string.Empty;
    public string? Comprobante { get; set; }
    public string? Num_Documento { get; set; }
    public string Razon_Social { get; set; } = string.Empty;
    public string NCF { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal ITBIS { get; set; }
    public decimal Descuento { get; set; }
}