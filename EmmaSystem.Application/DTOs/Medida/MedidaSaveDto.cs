using System.ComponentModel.DataAnnotations;

namespace EmmaSystem.Application.DTOs.Medida;

public sealed class MedidaSaveDto
{
    [Required, StringLength(20)]
    public string Mayor { get; init; } = default!;

    [Required, StringLength(20)]
    public string Detalle { get; init; } = default!;

    [Required]
    public decimal Contenido { get; init; } = 1;

    [StringLength(100)]
    public string? Descripcion { get; init; }
}