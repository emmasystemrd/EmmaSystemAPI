namespace EmmaSystem.Application.DTOs.Venta;

/// <summary>
/// DTO para ventas pendientes de cobro (spmostrar_venta1_pendiente / spbuscar_venta1_pendiente).
/// </summary>
public sealed class VentaPendienteDto
{
    public int Idventa1 { get; init; }
    public DateTime Fecha { get; init; }
    public string Nombre_Cliente { get; init; } = default!;
    public decimal Total { get; init; }
}