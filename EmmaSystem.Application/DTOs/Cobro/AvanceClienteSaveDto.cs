namespace EmmaSystem.Application.DTOs.Cobro;

public class AvanceClienteSaveDto
{
    public string Codigo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int Idcliente { get; set; }

    // Formas de pago (se guardan como negativos en la BD)
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
}