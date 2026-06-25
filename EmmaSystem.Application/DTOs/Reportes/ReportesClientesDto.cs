namespace EmmaSystem.Application.DTOs.Reportes;

// ═══ REPORTE: SALDOS POR ANTIGÜEDAD ═══
public class SaldosAntiguedadReporteDto
{
    public string Num_Documento { get; set; } = string.Empty;
    public string Razon_Social { get; set; } = string.Empty;
    public DateTime? UltCobro { get; set; }
    public decimal No_Vencida { get; set; }
    public decimal dias_30 { get; set; }
    public decimal dias_60 { get; set; }
    public decimal dias_90 { get; set; }
    public decimal dias_120 { get; set; }
    public decimal mas_120 { get; set; }
    public decimal Balance { get; set; }
}

// ═══ REPORTE: MOVIMIENTOS DE CLIENTE ═══
public class MovimientoClienteReporteDto
{
    public DateTime FECHA { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string Vence { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public decimal Pago { get; set; }
    public decimal Balance { get; set; }
}

// ═══ REPORTE: RECIBOS DE COBRO ═══
public class ReciboCobroReporteDto
{
    public int Idcobro { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Num_Documento { get; set; } = string.Empty;
    public string Razon_Social { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal Descuento { get; set; }
    public decimal Retencion_ITBIS { get; set; }
    public decimal Retencion_ISR { get; set; }
    public decimal Valor { get; set; }
    public decimal Pendiente { get; set; }
    public decimal Efectivo { get; set; }
    public decimal Cheque { get; set; }
    public int? Banco_Ck { get; set; }
    public string? Num_Ck { get; set; }
    public decimal Transferencia { get; set; }
    public int? Banco_Transf { get; set; }
    public string? Ref_Transf { get; set; }
    public decimal Tarjeta { get; set; }
    public int? Tipo_Tarjeta { get; set; }
    public string? Ref_Tarjeta { get; set; }
    public decimal Devuelta { get; set; }
}

// ═══ REPORTE: ESTADO DE CUENTA (COMPUESTO) ═══
public class EstadoCuentaReporteDto
{
    // Datos del cliente
    public string Razon_Social { get; set; } = string.Empty;
    public string? Num_Documento { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string Condicion { get; set; } = string.Empty;
    public decimal Limite { get; set; }

    // Saldos por antigüedad
    public decimal No_Vencida { get; set; }
    public decimal dias_30 { get; set; }
    public decimal dias_60 { get; set; }
    public decimal dias_90 { get; set; }
    public decimal dias_120 { get; set; }
    public decimal mas_120 { get; set; }
    public decimal BalanceTotal => No_Vencida + dias_30 + dias_60 + dias_90 + dias_120 + mas_120;

    // Movimientos
    public List<MovimientoClienteReporteDto> Movimientos { get; set; } = new();
}