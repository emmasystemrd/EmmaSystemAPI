namespace EmmaSystem.Application.DTOs.Venta;

public class VentaDetalleItemDto
{
    public int Iddetalle { get; set; }
    public int Idventa1 { get; set; }
    public int Idarticulo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string Medida { get; set; } = string.Empty;
    public decimal Precio_Venta1 { get; set; }
    public decimal P_Itbis { get; set; }
    public decimal Descuento { get; set; }
    public decimal ITBIS { get; set; }  // Calculado: Cantidad * Precio * P_Itbis
    public decimal Subtotal { get; set; }  // Calculado: Cantidad * Precio
}