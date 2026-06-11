namespace EmmaSystem.Application.DTOs.Medida;

/// <summary>
/// DTO simplificado para combos/selectores en el módulo de Artículos.
/// Mapea spmostrar_medida_articulo: devuelve nombre formateado "Mayor (Contenido)".
/// </summary>
public sealed class MedidaArticuloDto
{
    public int Idmedida { get; init; }
    public string Nombre { get; init; } = default!; // Formato: "Caja (24)"
}