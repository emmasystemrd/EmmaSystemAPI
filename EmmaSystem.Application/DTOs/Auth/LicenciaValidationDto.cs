namespace EmmaSystem.Application.DTOs.Auth;

public class ValidarLicenciaRequestDto
{
    public int IdCliente { get; set; }
    public byte[] SecretKey { get; set; } = Array.Empty<byte>();
}

public class LicenciaValidationResultDto
{
    public bool EsValida { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime? UltimaValidacion { get; set; }
    public int? DiasGraciaRestantes { get; set; }
}