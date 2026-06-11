namespace EmmaSystem.Application.DTOs.Articulo;

/// <summary>
/// DTO que mapea el resultado de spbuscar_detalle_producto_precio.
/// Se usa para consultar precios de presentaciones específicas de un artículo.
/// </summary>
public sealed class DetalleProductoPrecioDto
{
    public int Iddetalle { get; init; }
    public string Nombre { get; init; } = default!;
    public decimal Precio { get; init; }
    public string Medida { get; init; } = default!;
}