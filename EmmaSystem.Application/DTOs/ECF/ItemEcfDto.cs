namespace EmmaSystem.Application.DTOs.Ecf;

public class ItemEcfDto
{
    public int IndicadorFacturacion { get; set; } // 1=Gravado 18%, 2=Gravado 16%, 3=Gravado 0%, 4=Exento
    public string NombreItem { get; set; } = string.Empty;
    public string DescripcionItem { get; set; } = string.Empty;
    public int IndicadorBienoServicio { get; set; } // 1=Bien, 2=Servicio
    public decimal CantidadItem { get; set; }
    public int UnidadMedida { get; set; }
    public decimal PrecioUnitarioItem { get; set; }
    public decimal MontoItem { get; set; }

    // Descuentos (opcionales)
    public decimal? DescuentoMonto { get; set; }
    public string? TipoSubDescuento { get; set; } // "$" o "%"
    public decimal? MontoSubDescuento { get; set; }
    public decimal? SubDescuentoPorcentaje { get; set; }

    // Retenciones (opcionales)
    public string? IndicadorAgenteRetencionoPercepcion { get; set; }
    public decimal? MontoITBISRetenido { get; set; }
    public decimal? MontoISRRetenido { get; set; }

    // Otra moneda (opcionales)
    public decimal? PrecioOtraMoneda { get; set; }
    public decimal? MontoItemOtraMoneda { get; set; }
}