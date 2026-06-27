namespace EmmaSystem.Application.DTOs.Admin;

public class RegistrarBDExistenteRequestDto
{
    // ─── Datos del Cliente ───
    public string RazonSocial { get; set; } = string.Empty;
    public string? RNC { get; set; }
    public string CorreoPrincipal { get; set; } = string.Empty;
    public string? Telefono { get; set; }

    // ─── Datos del Usuario Central (Admin) ───
    public string EmailAdmin { get; set; } = string.Empty;
    public string PasswordAdmin { get; set; } = string.Empty;
    public string NombreCompletoAdmin { get; set; } = string.Empty;

    // ─── Datos de la Licencia ───
    public int IdPlan { get; set; }

    // ─── BD Existente ───
    public string NombreEmpresa { get; set; } = string.Empty;
    public string NombreBD { get; set; } = string.Empty;
    public string ServidorBD { get; set; } = ".\\EmmaSystem";
    public string? RncCedula { get; set; }
}