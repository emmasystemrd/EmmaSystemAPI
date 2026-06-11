namespace EmmaSystem.Application.DTOs.Venta;

/// <summary>
/// DTO para listado de ventas (spmostrar_venta).
/// </summary>
public sealed class VentaListadoDto
{
    public int Idventa1 { get; init; }
    public DateTime Fecha { get; init; }
    public string Codigo { get; init; } = default!;
    public string Nombre_Cliente { get; init; } = default!;
    public string NCF { get; init; } = default!;
    public decimal Subtotal { get; init; }
    public decimal ITBIS { get; init; }
    public decimal Total { get; init; }
}