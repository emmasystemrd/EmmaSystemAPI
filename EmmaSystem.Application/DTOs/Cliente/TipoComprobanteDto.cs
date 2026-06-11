namespace EmmaSystem.Application.DTOs.Cliente;

/// <summary>
/// DTO para tipos de comprobantes fiscales (NCF).
/// Mapea el resultado de [dbo].[spcargar_ecf].
/// </summary>
public sealed class TipoComprobanteDto
{
    public string Id { get; init; } = default!;      // Código: '01', '02', etc.
    public string Nombre { get; init; } = default!;  // Descripción: 'CRÉDITO FISCAL', etc.
}