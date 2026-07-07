namespace EmmaSystem.Application.DTOs.Auth;

public class TokenRenovadoConSesionDto
{
    public bool PuedeIniciar { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}