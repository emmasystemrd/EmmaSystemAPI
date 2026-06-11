using System.ComponentModel.DataAnnotations;

namespace EmmaSystem.Application.DTOs.Termino;

public sealed class TerminoSaveDto
{
    [Required, StringLength(20)]
    public string Nombre { get; init; } = default!;

    [Required, StringLength(1)]
    public string Tipo { get; init; } = default!; // Validar 'C' o 'P' en frontend

    public int Tiempo { get; init; }
    public decimal Tasa { get; init; }
    public int No_Pagos { get; init; }
    public int Dias_Desc { get; init; }
    public decimal P_Descuento { get; init; }
}