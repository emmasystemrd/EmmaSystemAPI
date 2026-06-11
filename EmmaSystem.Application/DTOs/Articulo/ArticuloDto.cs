namespace EmmaSystem.Application.DTOs.Articulo;

public sealed class ArticuloDto
{
    public int Idarticulo { get; init; }
    public string Codigo { get; init; } = default!;
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
    public string Tipo { get; init; } = default!; // P, V, C, A, M, T, R
    public decimal Costo { get; init; }
    public decimal Precio { get; init; }
    public decimal Tax { get; init; }
    public string Categoria { get; init; } = default!;
    public string Medida { get; init; } = default!;
    // ✅ CAMBIO: De decimal a string porque el SP devuelve '92.00 UNIDAD'
    public string Existencia { get; init; } = default!;
    public bool Facturar_Sin_Existencia { get; init; }
    public string Estado { get; init; } = default!;
}