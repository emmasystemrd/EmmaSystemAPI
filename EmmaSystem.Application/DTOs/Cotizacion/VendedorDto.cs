namespace EmmaSystem.Application.DTOs.Cotizacion;

public sealed class VendedorDto
{
    public int Idusuario { get; init; }
    public string Nombres { get; init; } = default!;
    public int Idempleado { get; init; }
}