using Microsoft.AspNetCore.Http;

namespace EmmaSystem.Application.DTOs.Empresa;

public class EmpresaUpdateRequest
{
    public string? Nombre { get; set; }
    public string? Tipo { get; set; }
    public string? RNC { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Url { get; set; }
    public string? Instagram { get; set; }
    public string? Facebook { get; set; }
    public string? Registrado { get; set; }
    public string? Fecha_Cierre { get; set; }
    public IFormFile? Logo { get; set; }
}