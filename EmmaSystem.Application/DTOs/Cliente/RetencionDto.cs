namespace EmmaSystem.Application.DTOs.Catalogos;

/// <summary>
/// DTO para retenciones (ITBIS/ISR).
/// Mapea el resultado de [dbo].[spcargar_retencion].
/// </summary>
public sealed class RetencionDto
{
    public int Idretencion { get; init; }
    public string Nombre { get; init; } = default!;
    public decimal Porcentaje { get; init; }
}