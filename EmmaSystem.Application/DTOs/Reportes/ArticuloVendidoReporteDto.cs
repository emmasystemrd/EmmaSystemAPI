namespace EmmaSystem.Application.DTOs.Reportes;

public class ArticuloVendidoReporteDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string Medida { get; set; } = string.Empty;
    public decimal Precio_Venta { get; set; }
    public decimal Total_Vendido { get; set; }
}