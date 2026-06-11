namespace EmmaSystem.Application.DTOs.Articulo;

public sealed class DetalleProductoDto
{
    public int Iddetalle { get; init; }
    public int Idarticulo { get; init; }
    public string? CodigoBarra { get; init; }
    public string Nombre { get; init; } = default!;
    public int Idmedida { get; init; }
    public string MedidaNombre { get; init; } = default!;
    public decimal Unidades { get; init; }
    public decimal Costo { get; init; }
    public decimal Margen { get; init; }
    public decimal Precio { get; init; }
    public decimal Existencia { get; init; }
}