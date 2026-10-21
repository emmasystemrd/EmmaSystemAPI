using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/bcrd")]
[Authorize]
public class BcrdController : ControllerBase
{
    private readonly IBcrdService _bcrdService;

    public BcrdController(IBcrdService bcrdService)
    {
        _bcrdService = bcrdService;
    }

    [HttpGet("tasas-cambio")]
    public async Task<IActionResult> ObtenerTasas(
        [FromQuery] DateTime fechaDesde,
        [FromQuery] DateTime fechaHasta)
    {
        var tasas = await _bcrdService.ObtenerTasasAsync(fechaDesde, fechaHasta);
        return Ok(tasas);
    }
}