using System.ComponentModel.DataAnnotations;

namespace EmmaSystem.Application.DTOs.Departamento;

public sealed class DepartamentoUpdateDto
{
    [Required, StringLength(50)] // El SP de editar acepta hasta 50
    public string Nombre { get; init; } = default!;

    [StringLength(100)]
    public string? Descripcion { get; init; }
}