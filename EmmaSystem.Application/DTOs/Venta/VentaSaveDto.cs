namespace EmmaSystem.Application.DTOs.Venta;

public class VentaSaveDto
{
    // ═══ INFORMACIÓN GENERAL ═══
    public DateTime Fecha { get; set; }
    public int Idcliente { get; set; }
    public string Nombre_Cliente { get; set; } = string.Empty;
    public string Contado { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string NCF { get; set; } = string.Empty;
    public int Idtermino { get; set; }
    public int Tipo_Ingreso { get; set; }

    // ═══ MONTOS ═══
    public decimal Subtotal { get; set; }
    public decimal ITBIS { get; set; }
    public decimal Descuento { get; set; }
    public decimal Monto_Descuento { get; set; }
    public DateTime? Vencimiento { get; set; }
    public decimal Interes { get; set; }
    public decimal Propina_Legal { get; set; }

    // ═══ INFORMACIÓN ADICIONAL ═══
    public string Descripcion { get; set; } = string.Empty;
    public string Cta_Ingreso { get; set; } = string.Empty;
    public decimal Monto_Servicios { get; set; }
    public decimal ITBIS_Servicios { get; set; }
    public int Iddepartamento { get; set; }
    public int Idlogin { get; set; }
    public int Idvendedor { get; set; }

    // ✅ NUEVO: Lista de detalles de venta (productos/servicios)
    public List<VentaDetalleItemDto> Detalles { get; set; } = new();
}