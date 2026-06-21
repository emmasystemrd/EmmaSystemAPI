// Application/DTOs/Auth/SeleccionEmpresaResponseDto.cs
namespace EmmaSystem.Application.DTOs.Auth;

public class SeleccionEmpresaResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int IdEmpresa { get; set; }
    public int ClienteId { get; set; }
    public string NombreEmpresa { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}