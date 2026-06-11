namespace EmmaSystem.Application.DTOs.Cobro;

public class DetalleCobroSaveDto
{
    public int Iddetalle { get; set; }
    public int Iddocumento { get; set; }
    public decimal Balance { get; set; }
    public decimal p_descuento { get; set; }
    public decimal Descuento { get; set; }
    public int Idretencion_ITBIS { get; set; }
    public int Idretencion_ISR { get; set; }
    public decimal p_isr { get; set; }
    public decimal p_itr { get; set; }
    public decimal isr { get; set; }
    public decimal itr { get; set; }
    public decimal Interes { get; set; }
    public decimal Cargos { get; set; }
    public decimal Monto { get; set; }
}