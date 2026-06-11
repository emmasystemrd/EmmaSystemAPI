namespace EmmaSystem.Application.DTOs.Ubicacion;

/// <summary>
/// DTO para municipios. Dependen de una provincia.
/// </summary>
public sealed class MunicipioDto
{
    public int Idmunicipio { get; init; }
    public string Municipio { get; init; } = default!;  // Ya viene en MAYÚSCULAS
}