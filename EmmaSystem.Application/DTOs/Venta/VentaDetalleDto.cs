namespace EmmaSystem.Application.DTOs.Venta;

/// <summary>
/// DTO completo para detalle de venta (spcargar_venta1_id / spcargar_venta1_ncf).
/// </summary>
public sealed class VentaDetalleDto
{
    public int Idventa1 { get; init; }
    public DateTime Fecha { get; init; }
    public string Codigo { get; init; } = default!;
    public string Nombre_Cliente { get; init; } = default!;
    public string Contado { get; init; } = default!;  // 'Y' o 'N'
    public string Tipo { get; init; } = default!;      // '01', '02', etc.
    public string NCF { get; init; } = default!;
    public int Idtermino { get; init; }
    public int Tipo_Ingreso { get; init; }
    public decimal Subtotal { get; init; }
    public decimal ITBIS { get; init; }
    public decimal Descuento { get; init; }  // Tasa decimal (0.10 = 10%)
    public decimal Monto_Descuento { get; init; }
    public decimal Interes { get; init; }
    public decimal Propina_Legal { get; init; }  // Tasa decimal
    public string Descripcion { get; init; } = default!;
    public string Cta_Ingreso { get; init; } = default!;
    public decimal Monto_Servicios { get; init; }
    public decimal ITBIS_Servicios { get; init; }
    public DateTime? Vencimiento { get; init; }
    public int Iddepartamento { get; init; }
    public int Idlogin { get; init; }  // Idempleado del usuario
    public int Idvendedor { get; init; }
    public int Idcliente { get; init; }
    public DateTime? FechaCreacion { get; init; }  // Para auditoría
}