using System.Text.Json.Serialization;

namespace EmmaSystem.Application.DTOs.Auth;

// ✅ DESPUÉS
public class ValidarLicenciaRequestDto
{
    public int IdCliente { get; set; }
    public string SecretKey { get; set; } = string.Empty;

    [JsonIgnore]
    public byte[] SecretKeyBytes => string.IsNullOrEmpty(SecretKey)
        ? Array.Empty<byte>()
        : Convert.FromBase64String(SecretKey);
}

public class LicenciaValidationResultDto
{
    public bool EsValida { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime? UltimaValidacion { get; set; }
    public int? DiasGraciaRestantes { get; set; }
}