using System.ComponentModel.DataAnnotations;

namespace EmmaSystem.Application.DTOs.Articulo;

public sealed class DetalleProductoSaveDto
{
    [Required]
    public int Idarticulo { get; init; }

    [StringLength(20)]
    public string? CodigoBarra { get; init; }

    [Required, StringLength(20)]
    public string Nombre { get; init; } = default!;

    [Required]
    public int Idmedida { get; init; }

    [Required]
    public decimal Unidades { get; init; } = 1;

    [Required]
    public decimal Costo { get; init; }

    [Required]
    public decimal Margen { get; init; }

    [Required]
    public decimal Precio { get; init; }
}