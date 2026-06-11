namespace EmmaSystem.Application.DTOs.Ubicacion;

/// <summary>
/// DTO para sectores. Dependen de un municipio.
/// </summary>
public sealed class SectorDto
{
    public int Idsector { get; init; }
    public string Sector { get; init; } = default!;  // Ya viene en MAYÚSCULAS
}