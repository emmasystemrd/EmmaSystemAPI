namespace EmmaSystem.Application.DTOs.Cobro;

public class CobroSaveDto
{
    public string Codigo { get; set; } = string.Empty;
    public int No_Cuota { get; set; }
    public DateTime Fecha { get; set; }
    public int Idcliente { get; set; }
    public decimal Balance { get; set; }
    public decimal Descuento { get; set; }
    public decimal Retencion_ITBIS { get; set; }
    public decimal Retencion_ISR { get; set; }

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

    // Detalles
    public List<DetalleCobroSaveDto> Detalles { get; set; } = new();
}