namespace EmmaSystem.Application.DTOs.Reportes;

// ═══ REPORTE: VENTA POR CENTRO DE COSTOS ═══
public class VentaDepartamentoReporteDto
{
    public DateTime Fecha { get; set; }
    public string Condicion { get; set; } = string.Empty;
    public string? Num_Documento { get; set; }
    public string Razon_Social { get; set; } = string.Empty;
    public string NCF { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal ITBIS { get; set; }
    public decimal Descuento { get; set; }
}

// ═══ REPORTE: UTILIDAD POR PRODUCTO ═══
public class UtilidadProductoReporteDto
{
    public string Cliente { get; set; } = string.Empty;
    public string NCF { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal Venta { get; set; }
    public decimal Costo { get; set; }
    public decimal Utilidad => Venta - Costo;
}

// ═══ REPORTE: COMISIÓN POR VENTAS ═══
public class ComisionVentaReporteDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Empleado { get; set; } = string.Empty;
    public decimal Porcentaje { get; set; }
    public decimal Ventas { get; set; }
    public decimal Comision { get; set; }
}

// ═══ REPORTE: COMISIÓN POR PRODUCTO ═══
public class ComisionProductoReporteDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public decimal Porcentaje { get; set; }
    public decimal Ventas { get; set; }
    public decimal Comision { get; set; }
}

// ═══ REPORTE: COTIZACIONES ═══
public class CotizacionReporteDto
{
    public DateTime Fecha { get; set; }
    public string No_Cotizacion { get; set; } = string.Empty;
    public string? Num_Documento { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal ITBIS { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public string Proceso { get; set; } = string.Empty;
}

// ═══ REPORTE: PEDIDOS ═══
public class PedidoReporteDto
{
    public DateTime Fecha { get; set; }
    public string No_Pedido { get; set; } = string.Empty;
    public string? Num_Documento { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal ITBIS { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public string Proceso { get; set; } = string.Empty;
}

// ═══ REPORTE: CONDUCE ═══
public class ConduceReporteDto
{
    public DateTime Fecha { get; set; }
    public string No_Conduce { get; set; } = string.Empty;
    public string? Num_Documento { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal ITBIS { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
}