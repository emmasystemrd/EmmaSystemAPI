using EmmaSystem.Application.DTOs.Admin;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
    }

    /// <summary>
    /// Valida todos los requisitos antes de registrar un cliente.
    /// Público: se llama desde el formulario de registro antes de enviar.
    /// </summary>
    [HttpPost("validar-registro")]
    [AllowAnonymous]
    public async Task<ActionResult<ValidarRegistroResponseDto>> ValidarRegistro(
        [FromBody] RegistrarClienteRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _adminService.ValidarRegistroAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Registra un nuevo cliente con usuario central, licencia y empresa.
    /// Todo en una sola transacción atómica con compensación.
    /// </summary>
    [HttpPost("registrar-cliente")]
    [AllowAnonymous] // Primer registro no requiere auth. Cambiar a [Authorize] en producción.
    public async Task<ActionResult<RegistrarClienteResponseDto>> RegistrarCliente(
        [FromBody] RegistrarClienteRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _adminService.RegistrarClienteAsync(request, cancellationToken);
            return Created($"/api/admin/clientes/{response.IdCliente}", response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error inesperado al registrar cliente: {ex.Message}" });
        }
    }
}