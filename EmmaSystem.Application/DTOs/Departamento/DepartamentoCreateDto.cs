using System.ComponentModel.DataAnnotations;

namespace EmmaSystem.Application.DTOs.Departamento;

public sealed class DepartamentoCreateDto
{
    [Required, StringLength(30)]
    public string Nombre { get; init; } = default!;

    [StringLength(100)]
    public string? Descripcion { get; init; }
}