namespace EmmaSystem.Application.DTOs.Cotizacion;

/// <summary>
/// DTO optimizado para impresión/reportes de cotizaciones.
/// Mapea exactamente las columnas que devuelve Imprimir_Cotizacion.
/// Dapper mapea case-insensitive, así que "num_documento" se asigna a Num_Documento automáticamente.
/// </summary>
public sealed class CotizacionImpresionDto
{
    public int Idcotizacion { get; init; }
    public string No_Cotizacion { get; init; } = default!;
    public DateTime Fecha { get; init; }
    public int Idcliente { get; init; }
    public string Razon_Social { get; init; } = default!;
    public string? Nombre_Comercial { get; init; }
    public int Tipo { get; init; }
    public string? Direccion { get; init; }
    public string Num_Documento { get; init; } = default!;
    public string Telefono { get; init; } = default!;
    public string? Descripcion { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Descuento { get; init; }
    public decimal ITBIS { get; init; }
    public decimal Total { get; init; }
}