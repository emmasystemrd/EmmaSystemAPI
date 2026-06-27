using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Empresa;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/empresa")]
[Authorize]
public sealed class EmpresaController : ControllerBase
{
    private readonly IEmpresaRepository _repo;

    public EmpresaController(IEmpresaRepository repo) => _repo = repo;

    private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")!.Value);

    /// <summary>
    /// Obtiene los datos de la empresa (sin el logo para mejor rendimiento)
    /// </summary>
    [HttpGet("{id:int}")]
    [Permission(Modules.System, Operations.Ver)]
    public async Task<ActionResult<EmpresaDto>> GetById(int id, CancellationToken ct)
    {
        var idEmpresaToken = GetIdEmpresa();
        if (idEmpresaToken != id) return Forbid();

        var result = await _repo.GetByIdAsync(id, ct);
        if (result is null) return NotFound(new { message = $"Empresa con ID {id} no encontrada." });

        return Ok(result);
    }

    /// <summary>
    /// Obtiene el logo de la empresa en base64
    /// </summary>
    [HttpGet("{id:int}/logo")]
    [Permission(Modules.System, Operations.Ver)]
    public async Task<IActionResult> GetLogo(int id, CancellationToken ct)
    {
        var idEmpresaToken = GetIdEmpresa();
        if (idEmpresaToken != id) return Forbid();

        var repo = _repo as EmpresaRepository;
        if (repo is null) return NotFound();

        var logo = await repo.GetLogoAsync(id, ct);
        if (logo is null || logo.Length == 0)
            return NotFound(new { message = "La empresa no tiene logo." });

        return Ok(new { logo = Convert.ToBase64String(logo) });
    }
    [HttpPut("{id:int}")]
    [Permission(Modules.System, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromForm] EmpresaUpdateRequest request, CancellationToken ct)
    {
        var idEmpresaToken = GetIdEmpresa();
        if (idEmpresaToken != id) return Forbid();

        try
        {
            // ✅ Conversión tolerante de Registrado
            bool registrado = false;
            if (!string.IsNullOrEmpty(request.Registrado))
            {
                var val = request.Registrado.Trim().ToLower();
                registrado = val == "true" || val == "1" || val == "si" || val == "yes";
            }

            // ✅ Conversión tolerante de Fecha_Cierre
            DateTime? fechaCierre = null;
            if (!string.IsNullOrWhiteSpace(request.Fecha_Cierre))
            {
                if (DateTime.TryParse(request.Fecha_Cierre, out var parsed))
                {
                    fechaCierre = parsed;
                }
            }

            var dto = new EmpresaDto
            {
                Idempresa = id,
                Nombre = request.Nombre?.Trim() ?? "",
                Tipo = request.Tipo?.Trim() ?? "",
                RNC = request.RNC?.Trim() ?? "",
                Direccion = request.Direccion?.Trim() ?? "",
                Telefono = request.Telefono?.Trim() ?? "",
                Email = request.Email?.Trim() ?? "",
                Url = request.Url?.Trim() ?? "",
                Instagram = request.Instagram?.Trim() ?? "",
                Facebook = request.Facebook?.Trim() ?? "",
                Registrado = registrado,
                Fecha_Cierre = fechaCierre
            };

            // ✅ Procesar logo solo si viene uno nuevo
            byte[]? logoBytes = null;
            if (request.Logo is not null && request.Logo.Length > 0)
            {
                using var ms = new MemoryStream();
                await request.Logo.CopyToAsync(ms, ct);
                logoBytes = ms.ToArray();
            }

            await _repo.UpdateAsync(dto, logoBytes, ct);
            return Ok(new { message = "Empresa actualizada correctamente." });
        }
        catch (Exception ex)
        {
            // ✅ Devolver error detallado para depurar
            return BadRequest(new
            {
                message = "Error al actualizar empresa",
                error = ex.Message,
                inner = ex.InnerException?.Message,
                received = new
                {
                    Nombre = request.Nombre,
                    Tipo = request.Tipo,
                    RNC = request.RNC,
                    Fecha_Cierre = request.Fecha_Cierre,
                    Registrado = request.Registrado,
                    HasLogo = request.Logo != null
                }
            });
        }

    }

    // DTO
    public class EmpresaConexionDto
    {
        public int IdEmpresa { get; set; }
        public string NombreEmpresa { get; set; } = string.Empty;
        public string CadenaConexion { get; set; } = string.Empty;
    }
}