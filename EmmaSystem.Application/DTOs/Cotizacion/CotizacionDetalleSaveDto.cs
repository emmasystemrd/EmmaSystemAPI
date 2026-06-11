using System.ComponentModel.DataAnnotations;

namespace EmmaSystem.Application.DTOs.Cotizacion;

public sealed class CotizacionDetalleSaveDto
{
    [Required]
    public int Idcotizacion { get; init; }

    [Required]
    public int Idarticulo { get; init; }

    [Required]
    public decimal Cantidad { get; init; }

    [Required, StringLength(20)]
    public string Medida { get; init; } = default!;

    [Required]
    public decimal Precio { get; init; }

    public decimal Itbis { get; init; }
    public decimal Descuento { get; init; }
}