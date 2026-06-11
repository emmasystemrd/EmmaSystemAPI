namespace EmmaSystem.Application.DTOs.Cotizacion;

/// <summary>
/// DTO que devuelve el siguiente número de cotización disponible para un tipo y empresa.
/// </summary>
public sealed class SecuenciaCotizacionDto
{
    public string Tipo { get; init; } = default!;
    public string SiguienteNumero { get; init; } = default!;
}