namespace EmmaSystem.Application.DTOs.Venta;

public class FacturaReporteDto
{
    public int Idventa1 { get; set; }
    public string Contado { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string NCF { get; set; } = string.Empty;
    public DateTime? Vencimiento { get; set; }
    public string Razon_Social { get; set; } = string.Empty;
    public string Nombre_Comercial { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Num_Documento { get; set; } = string.Empty; // Mapea a num_documento
    public string Telefono { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Cajero { get; set; } = string.Empty;
    public string Termino { get; set; } = string.Empty;
    public string Vendedor { get; set; } = string.Empty;
    public int Tiempo { get; set; }
    public decimal Descuento { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ITBIS { get; set; }
    public decimal Desc1 { get; set; }
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto_Servicios { get; set; }
    public decimal Propina_Legal { get; set; }
    public decimal Retencion_ITBIS { get; set; }
    public decimal Retencion_ISR { get; set; }
    public decimal ITBIS_Servicios { get; set; }
}

public class FacturaDetalleReporteDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string Medida { get; set; } = string.Empty;
    public decimal Precio_Venta1 { get; set; }
    public decimal Importe { get; set; }
    public decimal ITBIS { get; set; }
    public decimal Descuento { get; set; }
}