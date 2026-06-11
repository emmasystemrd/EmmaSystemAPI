namespace EmmaSystem.Application.DTOs.Ubicacion;

/// <summary>
/// DTO para provincias de República Dominicana.
/// Datos nacionales (no dependen de Idempresa).
/// </summary>
public sealed class ProvinciaDto
{
    public int Idprovincia { get; init; }
    public string Provincia { get; init; } = default!;  // Ya viene en MAYÚSCULAS desde SQL
}