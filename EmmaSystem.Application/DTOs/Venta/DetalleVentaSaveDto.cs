namespace EmmaSystem.Application.DTOs.Venta;

/// <summary>
/// DTO para detalle de venta (para insertar/actualizar líneas).
/// </summary>
public sealed class DetalleVentaSaveDto
{
    public int? Iddetalle { get; init; }  // Para actualización
    public int IdVenta1 { get; init; }
    public int Idarticulo { get; init; }  // Aquí va Iddetalle de Detalle_Producto
    public decimal Cantidad { get; init; }
    public string Medida { get; init; } = default!;
    public decimal Precio_Venta1 { get; init; }
    public decimal ITBIS { get; init; }  // Tasa decimal (0.00 - 1.00)
    public decimal Descuento { get; init; }  // Tasa decimal (0.00 - 1.00)
}