namespace EmmaSystem.Application.DTOs.Auth;

public class TokenRenovadoDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}