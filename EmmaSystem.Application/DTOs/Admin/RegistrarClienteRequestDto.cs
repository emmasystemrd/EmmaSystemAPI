namespace EmmaSystem.Application.DTOs.Admin;

public class RegistrarClienteRequestDto
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

    // ─── Empresas (array, compatible con el frontend) ───
    public List<EmpresaRegistroDto> Empresas { get; set; } = new();
}

public class EmpresaRegistroDto
{
    public string NombreEmpresa { get; set; } = string.Empty;
    public string NombreBD { get; set; } = string.Empty;
    public string ServidorBD { get; set; } = ".\\EmmaSystem";
    public string ConnectionString { get; set; } = string.Empty;
    public string? RncCedula { get; set; }
    public bool EsDefault { get; set; }
}

public class RegistrarClienteResponseDto
{
    public int IdCliente { get; set; }
    public string CodigoCliente { get; set; } = string.Empty;
    public int IdUsuarioCentral { get; set; }
    public int IdLicencia { get; set; }
    public int IdEmpresa { get; set; }
    public string Mensaje { get; set; } = "Cliente registrado exitosamente.";
}

public class ValidarRegistroResponseDto
{
    public bool EsValido { get; set; }
    public List<string> Errores { get; set; } = new();
}