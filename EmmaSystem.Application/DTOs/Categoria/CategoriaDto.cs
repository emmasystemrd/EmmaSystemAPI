namespace EmmaSystem.Application.DTOs.Categoria;

/// <summary>
/// DTO para listados y gestión administrativa de categorías.
/// Mapea exactamente las columnas de spmostrar_categoria / spbuscar_categoria.
/// </summary>
public sealed class CategoriaDto
{
    public int Idcategoria { get; init; }
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public string Tipo { get; init; } = default!; // A=Artículo, C=Cliente, P=Proveedor, etc.
}