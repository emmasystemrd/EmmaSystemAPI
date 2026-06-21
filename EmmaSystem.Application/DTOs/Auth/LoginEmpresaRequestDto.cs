namespace EmmaSystem.Application.DTOs.Auth;

public class LoginEmpresaRequestDto
{
    public string Usuario { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public int Idempresa { get; set; }
}