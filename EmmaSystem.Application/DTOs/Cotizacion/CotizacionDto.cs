namespace EmmaSystem.Application.DTOs.Cotizacion;

public sealed class CotizacionDto
{
    public int Idcotizacion { get; init; }
    public int Idcliente { get; init; }
    public string No_Cotizacion { get; init; } = default!;
    public DateTime Fecha { get; init; }
    public string? Num_Documento { get; init; }
    public string Razon_Social { get; init; } = default!;
    public string? Descripcion { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Descuento { get; init; }
    public decimal ITBIS { get; init; }
    public decimal Total { get; init; }
    public string Proceso { get; init; } = default!; // A=Abierta, E=Anulada, C=Cerrada
}