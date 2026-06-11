namespace EmmaSystem.Application.DTOs.Cotizacion;

public sealed class CotizacionDetalleDto
{
    public int Iddetalle { get; init; }
    public int Iddetalle_Producto { get; init; }
    public int Idarticulo { get; init; }
    public decimal Cantidad { get; init; }
    public string Medida { get; init; } = default!;
    public string Producto { get; init; } = default!;
    public decimal Precio { get; init; }
    public decimal Subtotal { get; init; }
    public decimal ITBIS { get; init; }
    public decimal P_Itbis { get; init; }
    public decimal Descuento { get; init; }
    public int Contenido { get; init; }
    public bool Tax { get; init; }
    public string Codigo { get; init; } = default!;
    public string Mayor { get; init; } = default!;
    public string Detalle { get; init; } = default!;
}