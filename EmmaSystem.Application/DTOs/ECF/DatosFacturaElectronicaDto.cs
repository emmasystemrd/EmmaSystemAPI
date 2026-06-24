namespace EmmaSystem.Application.DTOs.Ecf;

public class DatosFacturaElectronicaDto
{
    // Obligatorios
    public string TipoComprobante { get; set; } = string.Empty; // 31, 32, 44, 45, 46
    public string ENCF { get; set; } = string.Empty;
    public string RNCEmisor { get; set; } = string.Empty;

    // IdDoc
    public string? FechaVencimientoSecuencia { get; set; }
    public string? IndicadorNotaCredito { get; set; }
    public string? TipoIngresos { get; set; }
    public string? IndicadorEnvioDiferido { get; set; }
    public string? IndicadorMontoGravado { get; set; }
    public string? IndicadorServicioTodoIncluido { get; set; }
    public string? TipoPago { get; set; } // "1"=Contado, "2"=Crédito
    public string? FechaLimitePago { get; set; }
    public string? TerminoPago { get; set; }
    public List<FormaPagoEcfDto>? FormasPago { get; set; }

    // Emisor
    public string RazonSocialEmisor { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string? DireccionEmisor { get; set; }
    public string? Municipio { get; set; }
    public string? Provincia { get; set; }
    public List<string>? TelefonosEmisor { get; set; }
    public string? CorreoEmisor { get; set; }
    public string? WebSite { get; set; }
    public string FechaEmision { get; set; } = string.Empty;

    // Comprador
    public string? RNCComprador { get; set; }
    public string? RazonSocialComprador { get; set; }
    public string? IdentificadorExtranjero { get; set; }
    public string? CorreoComprador { get; set; }
    public string? DireccionComprador { get; set; }
    public string? TelefonoAdicional { get; set; }

    // Transporte
    public string? PaisDestino { get; set; }

    // Totales
    public decimal? MontoGravadoTotal { get; set; }
    public decimal? MontoGravadoI1 { get; set; } // 18%
    public decimal? MontoGravadoI2 { get; set; } // 16%
    public decimal? MontoGravadoI3 { get; set; }
    public decimal? MontoExento { get; set; }
    public string? ITBIS1 { get; set; } = "18";
    public string? ITBIS2 { get; set; } = "16";
    public string? ITBIS3 { get; set; } = "0";
    public decimal? TotalITBIS { get; set; }
    public decimal? TotalITBIS1 { get; set; }
    public decimal? TotalITBIS2 { get; set; }
    public decimal? TotalITBIS3 { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal ValorPagar { get; set; }
    public decimal TotalITBISRetenido { get; set; }
    public decimal TotalISRRetencion { get; set; }

    // Otra moneda
    public string? TipoMoneda { get; set; }
    public decimal? TipoCambio { get; set; }
    public decimal? MontoExentoOtraMoneda { get; set; }
    public decimal? MontoTotalOtraMoneda { get; set; }

    // Items
    public List<ItemEcfDto> Items { get; set; } = new();

    // Referencia (para NC/ND)
    public string? NCFModificado { get; set; }
    public string? FechaNCFModificado { get; set; }
    public string? CodigoModificacion { get; set; }
    public string? RazonModificacion { get; set; }

    // Firma
    public string FechaHoraFirma { get; set; } = string.Empty;
    public string CodigoSeguridad { get; set; } = string.Empty;
}