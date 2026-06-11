using System.ComponentModel.DataAnnotations;

namespace EmmaSystem.Application.DTOs.Categoria;

public sealed class CategoriaSaveDto
{
    [Required, StringLength(50)]
    public string Nombre { get; init; } = default!;

    [StringLength(100)]
    public string? Descripcion { get; init; }

    [Required, StringLength(1)]
    public string Tipo { get; init; } = "A"; // Default a Artículo
}