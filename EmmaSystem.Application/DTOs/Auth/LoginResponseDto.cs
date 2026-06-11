namespace EmmaSystem.Application.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
    public int Idusuario { get; set; }
    public int Idempresa { get; set; }
    public int Idacceso { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Puesto { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public byte[] Foto { get; set; }
    public IReadOnlyList<int> Idroles { get; set; } = Array.Empty<int>();
}