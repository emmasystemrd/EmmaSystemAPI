namespace EmmaSystem.Application.DTOs.Cliente;

public sealed class TutorBusquedaDto
{
    public int Idcliente { get; init; }
    public string? Codigo { get; init; }
    public string? Razon_Social { get; init; }
    public string? Nombre_Comercial { get; init; }
    public string? Num_Documento { get; init; }
    public string? Telefono { get; init; }
    public string? Email { get; init; }
    public string? Direccion { get; init; }
}