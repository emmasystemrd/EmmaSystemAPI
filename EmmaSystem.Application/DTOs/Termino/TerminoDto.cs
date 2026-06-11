namespace EmmaSystem.Application.DTOs.Termino;

/// <summary>
/// DTO para listados y gestión administrativa de términos de pago.
/// Mapea exactamente las columnas de spmostrar_termino / spbuscar_termino.
/// </summary>
public sealed class TerminoDto
{
    public int Idtermino { get; init; }
    public string Nombre { get; init; } = default!;
    public string Tipo { get; init; } = default!; // C=Cliente, P=Proveedor
    public int No_Pagos { get; init; }
    public decimal Tasa { get; init; }
    public int Tiempo { get; init; }
    public int Dias_Desc { get; init; }
    public decimal P_Descuento { get; init; }
}