namespace EmmaSystem.Application.DTOs.Configuracion;

public class ENcfDto
{
    public int IdeNCF { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int Desde { get; set; }
    public int Hasta { get; set; }
    public string Secuencia { get; set; } = string.Empty;
    public int Aviso { get; set; }
    public DateTime? Vencimiento { get; set; } // Nullable por la lógica del SP
}