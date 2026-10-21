namespace EmmaSystem.Application.DTOs.BCRD;

public class TasaCambioDto
{
    public decimal Compra { get; set; }
    public decimal Venta { get; set; }
    public int MonedaId { get; set; }
    public DateTime Fecha { get; set; }
}