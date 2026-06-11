using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Dgii;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/dgii")]
[Authorize]
public class DgiiController : ControllerBase
{
    private readonly IDgiiService _dgiiService;
    private readonly ILogger<DgiiController> _logger;

    public DgiiController(IDgiiService dgiiService, ILogger<DgiiController> logger)
    {
        _dgiiService = dgiiService;
        _logger = logger;
    }

    /// <summary>
    /// Consulta contribuyente por RNC o Cédula en la DGII.
    /// </summary>
    [HttpGet("contribuyente/{rncOCedula}")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<ContribuyenteDto>> ConsultarContribuyente(
        string rncOCedula, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rncOCedula))
            return BadRequest(new { message = "El RNC/Cédula es requerido." });

        var result = await _dgiiService.ConsultarRncAsync(rncOCedula);

        if (result is null)
            return NotFound(new { message = $"No se encontró información para {rncOCedula} en la DGII." });

        return Ok(result);
    }

    /// <summary>
    /// Consulta RNC registrado en la DGII (información básica)
    /// </summary>
    [HttpGet("rnc-registrado/{rncOCedula}")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<RncRegistradoDto>> ConsultarRncRegistrado(
        string rncOCedula, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rncOCedula))
            return BadRequest(new { message = "El RNC/Cédula es requerido." });

        var result = await _dgiiService.ConsultarRncRegistradoAsync(rncOCedula);

        if (result is null)
            return NotFound(new
            {
                message = $"No se encontró el RNC {rncOCedula} en los registros de la DGII."
            });

        return Ok(result);
    }

    /// <summary>
    /// Valida un NCF en la DGII.
    /// </summary>
    [HttpGet("ncf/{ncf}/emisor/{rncEmisor}")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<NcfValidationDto>> ConsultarNcf(
        string ncf, string rncEmisor, CancellationToken ct)
    {
        var result = await _dgiiService.ConsultarNcfAsync(ncf, rncEmisor);

        if (result is null)
            return NotFound(new { message = $"NCF {ncf} no encontrado o inválido." });

        return Ok(result);
    }

    /// <summary>
    /// Valida un e-NCF en la DGII.
    /// </summary>
    [HttpPost("encf/validar")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<ENcfValidationDto>> ConsultarENcf(
        [FromBody] ENcfRequestDto request, CancellationToken ct)
    {
        var result = await _dgiiService.ConsultarENcfAsync(
            request.RncEmisor, request.ENcf, request.RncComprador, request.CodigoSeguridad);

        if (result is null)
            return NotFound(new { message = "e-NCF no encontrado o datos incorrectos." });

        return Ok(result);
    }
}

public class ENcfRequestDto
{
    public string RncEmisor { get; set; } = string.Empty;
    public string ENcf { get; set; } = string.Empty;
    public string RncComprador { get; set; } = string.Empty;
    public string CodigoSeguridad { get; set; } = string.Empty;
}