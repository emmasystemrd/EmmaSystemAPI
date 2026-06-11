namespace EmmaSystem.Application.DTOs.Cobro;

public class CobroListadoDto
{
    public int Idcobro { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Razon_Social { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal Descuento { get; set; }
    public decimal Valor { get; set; }
    public decimal Pendiente { get; set; }
    public bool TienePDF { get; set; }
}