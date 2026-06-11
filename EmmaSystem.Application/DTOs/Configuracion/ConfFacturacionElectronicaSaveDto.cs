namespace EmmaSystem.Application.DTOs.Configuracion;

public class ConfFacturacionElectronicaSaveDto
{
    public int Envio_Inmediato { get; set; }
    public int? Ambiente { get; set; }
    public byte[]? Certificado_Digital { get; set; }
    public string? Clave { get; set; }
    public string? Email { get; set; }
    public string? Clave_Email { get; set; }
    public DateTime? FechaExpira { get; set; }
}