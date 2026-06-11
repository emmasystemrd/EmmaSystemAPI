namespace EmmaSystem.Application.DTOs.Cobro;

public class CobroDetalleDto
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

    // Formas de pago
    public decimal Efectivo { get; set; }
    public decimal Cheque { get; set; }
    public int? Banco_Ck { get; set; }
    public string Num_Ck { get; set; } = string.Empty;
    public decimal Transferencia { get; set; }
    public int? Banco_Transf { get; set; }
    public string Ref_Transf { get; set; } = string.Empty;
    public decimal Tarjeta { get; set; }
    public int? Tipo_Tarjeta { get; set; }
    public string Ref_Tarjeta { get; set; } = string.Empty;
    public decimal Devuelta { get; set; }

    // Metadatos
    public int Idcliente { get; set; }
    public int No_Cuota { get; set; }
    public bool EsAvance => Codigo.StartsWith("AC-");

    // Detalles aplicados
    public List<DetalleCobroDto> Detalles { get; set; } = new();
}