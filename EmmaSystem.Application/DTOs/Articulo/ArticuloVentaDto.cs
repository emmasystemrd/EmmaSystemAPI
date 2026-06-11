namespace EmmaSystem.Application.DTOs.Articulo;

/// <summary>
/// DTO optimizado para el buscador de artículos en Ventas.
/// Mapea exactamente las columnas que devuelve spbuscararticulo_venta1.
/// </summary>
public sealed class ArticuloVentaDto
{
    public int IdArticulo { get; init; }
    public string Código { get; init; } = default!;
    public string Artículo { get; init; } = default!;
    public string Tipo { get; init; } = default!;
    public decimal Precio { get; init; }
    public string Existencia { get; init; } = default!; // Formato: "50.00 UNIDAD"
    public decimal Exist { get; init; }                 // Valor numérico puro
    public decimal Contenido { get; init; }
    public int Maximo { get; init; }
    public int Minimo { get; init; }
    public decimal Gravado { get; init; }                  // Mapeado de A1.Tax
    public string Mayor { get; init; } = default!;
    public string Detalle { get; init; } = default!;
    public string Facturar_Sin_Existencia { get; init; } = default!;
    public string? Foto { get; init; }
}