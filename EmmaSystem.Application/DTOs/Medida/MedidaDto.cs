namespace EmmaSystem.Application.DTOs.Medida;

/// <summary>
/// DTO para listados y gestión administrativa de medidas.
/// Mapea exactamente las columnas de spmostrar_medida / spbuscar_medida.
/// </summary>
public sealed class MedidaDto
{
    public int Idmedida { get; init; }
    public string Mayor { get; init; } = default!;       // Nombre principal (ej: "Caja")
    public string Detalle { get; init; } = default!;     // Abreviatura (ej: "CJ")
    public decimal Contenido { get; init; }              // Factor de conversión (ej: 24.00000000)
    public string? Descripcion { get; init; }
}