namespace EmmaSystem.Application.DTOs.Configuracion;

public class ConfFacturacionDto
{
    public int Idconf { get; set; }
    public int? Tipo { get; set; }
    public bool? Vista_Previa { get; set; }
    public decimal? Ancho_Papel { get; set; }
    public decimal? Margen_Papel { get; set; }
    public string? Impresora { get; set; }
    public string? Mensaje { get; set; }
    public int? Copia { get; set; }
    public decimal? Propina_Legal { get; set; }
    public string? Cod_Propina_Legal { get; set; }
    public int? ITBIS_Incluido { get; set; }
}