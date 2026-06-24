namespace EmmaSystem.Application.DTOs.Ecf;

public class FormaPagoEcfDto
{
    public string FormaPago { get; set; } = string.Empty; // "1"=Efectivo, "2"=Crédito, etc.
    public decimal MontoPago { get; set; }
}