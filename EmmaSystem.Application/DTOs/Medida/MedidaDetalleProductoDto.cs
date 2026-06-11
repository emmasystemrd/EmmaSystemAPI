namespace EmmaSystem.Application.DTOs.Medida;

/// <summary>
/// DTO que mapea el resultado de spmostrar_medida_detalle_producto.
/// Nota: El SP devuelve "Mayor as Nombre", Dapper lo mapea automáticamente a la propiedad Nombre.
/// </summary>
public sealed class MedidaDetalleProductoDto
{
    public int Idmedida { get; init; }
    public string Nombre { get; init; } = default!;
    public decimal Unidades { get; init; }
}