namespace EmmaSystem.Application.DTOs.Configuracion;

public class ConfFacturacionElectronicaDto
{
    public int Id { get; set; }
    public int Envio_Inmediato { get; set; }
    public int? Ambiente { get; set; }
    public string? Email { get; set; }
    public DateTime? FechaExpira { get; set; }
    public bool TieneCertificado { get; set; }
    public bool TieneClaveCertificado { get; set; }
    public bool TieneClaveEmail { get; set; }
}