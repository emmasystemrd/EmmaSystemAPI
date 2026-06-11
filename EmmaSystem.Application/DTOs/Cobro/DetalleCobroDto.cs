namespace EmmaSystem.Application.DTOs.Cobro;

public class DetalleCobroDto
{
    public int Iddetalle { get; set; }
    public int Iddocumento { get; set; }
    public DateTime Fecha { get; set; }
    public string Detalle { get; set; } = string.Empty;
    public decimal Saldo { get; set; }
    public decimal p_descuento { get; set; }
    public decimal Descuento { get; set; }
    public decimal p_itr { get; set; }
    public decimal itr { get; set; }
    public decimal p_isr { get; set; }
    public decimal isr { get; set; }
    public decimal Interes { get; set; }
    public decimal Cargos { get; set; }
    public decimal Pago { get; set; }
    public bool Seleccionar { get; set; }
    public decimal Pendiente { get; set; }
    public decimal itbis { get; set; }
    public decimal subtotal { get; set; }
    public int Idretencion_ITBIS { get; set; }
    public int Idretencion_ISR { get; set; }
    public decimal Monto { get; set; }
}