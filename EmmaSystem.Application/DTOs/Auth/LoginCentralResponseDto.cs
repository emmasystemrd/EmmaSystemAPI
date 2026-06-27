// Application/DTOs/Auth/LoginCentralResponseDto.cs
namespace EmmaSystem.Application.DTOs.Auth;

public class LoginCentralResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int IdCliente { get; set; } // ← AGREGADO
    public byte[] SecretKey { get; set; } = Array.Empty<byte>(); // ← AGREGADO
    public string NombreCliente { get; set; } = string.Empty;
    public List<EmpresaDisponibleDto> Empresas { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
    public bool AutoSeleccionar { get; set; }
}

public class EmpresaDisponibleDto
{
    public int IdEmpresa { get; set; }
    public string NombreEmpresa { get; set; } = string.Empty;
    public bool EsDefault { get; set; }
}