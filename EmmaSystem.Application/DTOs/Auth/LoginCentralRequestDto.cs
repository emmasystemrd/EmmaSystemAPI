// Application/DTOs/Auth/LoginCentralRequestDto.cs
namespace EmmaSystem.Application.DTOs.Auth;

public class LoginCentralRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string NombreEquipo { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
}

