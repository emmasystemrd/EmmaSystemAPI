namespace EmmaSystem.Application.DTOs.Departamento;

public sealed class DepartamentoDto
{
    public int Iddepartamento { get; init; }
    public string Nombre { get; init; } = default!;
    public string? Descripcion { get; init; }
}