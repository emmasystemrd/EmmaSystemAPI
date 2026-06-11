namespace EmmaSystem.Application.DTOs.Empresa;

public class EmpresaDto
{
    public int Idempresa { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string RNC { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Instagram { get; set; } = string.Empty;
    public string Facebook { get; set; } = string.Empty;
    public bool Registrado { get; set; }
    public DateTime? Fecha_Cierre { get; set; }
    public string? LogoBase64 { get; set; }  // Logo en base64 para el frontend
    public bool TieneLogo { get; set; }
}