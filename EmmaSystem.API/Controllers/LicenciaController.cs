using EmmaSystem.Application.DTOs.Auth;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/licencias")]
public class LicenciaController : ControllerBase
{
    private readonly ILicenciaService _licenciaService;

    public LicenciaController(ILicenciaService licenciaService)
    {
        _licenciaService = licenciaService;
    }

    [HttpPost("validar")]
    [AllowAnonymous]
    public async Task<ActionResult<LicenciaValidationResultDto>> Validar(
        [FromBody] ValidarLicenciaRequestDto request,
        CancellationToken ct)
    {
        var result = await _licenciaService.ValidarAsync(request.IdCliente, request.SecretKey, ct);
        return Ok(result);
    }
}