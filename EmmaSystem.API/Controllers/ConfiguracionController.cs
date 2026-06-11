using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Configuracion;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/configuracion")]
[Authorize]
public sealed class ConfiguracionController : ControllerBase
{
    private readonly IConfiguracionRepository _repo;

    public ConfiguracionController(IConfiguracionRepository repo) => _repo = repo;

    private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")?.Value ?? "1");

    // ═══════════════════════════════════════════════════════════════
    // FACTURACIÓN ELECTRÓNICA
    // ═══════════════════════════════════════════════════════════════

    // ✅ Para facturación electrónica
    [HttpGet("facturacion-electronica")]
    [Permission(Modules.System, Operations.Ver)]
    public async Task<ActionResult<ConfFacturacionElectronicaDto>> GetFacturacionElectronica(CancellationToken ct)
    {
        try
        {
            var result = await _repo.GetFacturacionElectronicaAsync(GetIdEmpresa(), ct);
            if (result is null)
                return Ok(new ConfFacturacionElectronicaDto());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Error al cargar configuración de facturación electrónica",
                error = ex.Message
            });
        }
    }

    [HttpPut("facturacion-electronica")]
    [Permission(Modules.System, Operations.Editar)]
    public async Task<IActionResult> UpdateFacturacionElectronica([FromBody] ConfFacturacionElectronicaSaveDto dto, CancellationToken ct)
    {
        await _repo.UpdateFacturacionElectronicaAsync(dto, GetIdEmpresa(), ct);
        return Ok(new { message = "Configuración de facturación electrónica actualizada." });
    }

    // ═══════════════════════════════════════════════════════════════
    // PROVEEDOR / CLIENTE
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("proveedor-cliente")]
    [Permission(Modules.System, Operations.Ver)]
    public async Task<ActionResult<ConfProveedorClienteDto>> GetProveedorCliente(CancellationToken ct)
    {
        var result = await _repo.GetProveedorClienteAsync(GetIdEmpresa(), ct);
        return Ok(result ?? new ConfProveedorClienteDto());
    }

    [HttpPut("proveedor-cliente")]
    [Permission(Modules.System, Operations.Editar)]
    public async Task<IActionResult> UpdateProveedorCliente([FromBody] ConfProveedorClienteDto dto, CancellationToken ct)
    {
        await _repo.UpdateProveedorClienteAsync(dto, GetIdEmpresa(), ct);
        return Ok(new { message = "Configuración proveedor/cliente actualizada." });
    }

    // ═══════════════════════════════════════════════════════════════
    // CAPITAL
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("capital")]
    [Permission(Modules.System, Operations.Ver)]
    public async Task<ActionResult> GetCapital(CancellationToken ct)
    {
        var (capital, cuentaCierre) = await _repo.GetCapitalAsync(GetIdEmpresa(), ct);
        return Ok(new { capital, cuentaCierre });
    }

    [HttpPut("capital")]
    [Permission(Modules.System, Operations.Editar)]
    public async Task<IActionResult> UpdateCapital([FromBody] CapitalDto dto, CancellationToken ct)
    {
        await _repo.UpdateCapitalAsync(dto.capital ?? "", dto.cuentaCierre ?? "", GetIdEmpresa(), ct);
        return Ok(new { message = "Configuración de capital actualizada." });
    }

    // ═══════════════════════════════════════════════════════════════
    // IMPUESTOS
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("impuestos")]
    [Permission(Modules.System, Operations.Ver)]
    public async Task<ActionResult<ConfImpuestoDto>> GetImpuestos(CancellationToken ct)
    {
        var result = await _repo.GetImpuestosAsync(GetIdEmpresa(), ct);
        return Ok(result ?? new ConfImpuestoDto());
    }

    // ═══════════════════════════════════════════════════════════════
    // EMPLEADOS
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("empleado")]
    [Permission(Modules.System, Operations.Ver)]
    public async Task<ActionResult<ConfEmpleadoDto>> GetEmpleado(CancellationToken ct)
    {
        var result = await _repo.GetEmpleadoAsync(GetIdEmpresa(), ct);
        return Ok(result ?? new ConfEmpleadoDto());
    }

    // ═══════════════════════════════════════════════════════════════
    // TSS
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("tss")]
    [Permission(Modules.System, Operations.Ver)]
    public async Task<ActionResult<ConfTssDto>> GetTss(CancellationToken ct)
    {
        var result = await _repo.GetTssAsync(GetIdEmpresa(), ct);
        return Ok(result ?? new ConfTssDto());
    }

    // ═══════════════════════════════════════════════════════════════
    // FACTURACIÓN (IMPRESIÓN)
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("facturacion")]
    [Permission(Modules.System, Operations.Ver)]
    public async Task<ActionResult<ConfFacturacionDto>> GetFacturacion(CancellationToken ct)
    {
        var result = await _repo.GetFacturacionAsync(GetIdEmpresa(), ct);
        return Ok(result ?? new ConfFacturacionDto());
    }

    [HttpPut("facturacion")]
    [Permission(Modules.System, Operations.Editar)]
    public async Task<IActionResult> UpdateFacturacion([FromBody] ConfFacturacionDto dto, CancellationToken ct)
    {
        await _repo.UpdateFacturacionAsync(dto, ct);
        return Ok(new { message = "Configuración de facturación actualizada." });
    }
}

// DTO auxiliar para Capital
public class CapitalDto
{
    public string? capital { get; set; }
    public string? cuentaCierre { get; set; }
}