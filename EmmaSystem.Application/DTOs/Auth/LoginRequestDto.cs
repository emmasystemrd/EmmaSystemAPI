using System.ComponentModel.DataAnnotations;

namespace EmmaSystem.Application.DTOs.Auth;

public sealed class LoginRequestDto
{
    [Required]
    [StringLength(50)]
    public string Usuario { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Clave { get; set; } = string.Empty;

    [Required]
    public int Idempresa { get; set; }
}