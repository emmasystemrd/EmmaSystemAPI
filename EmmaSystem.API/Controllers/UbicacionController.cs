using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Ubicacion;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/ubicacion")]
[Authorize]
public sealed class UbicacionController : ControllerBase
{
    private readonly IUbicacionRepository _repo;

    public UbicacionController(IUbicacionRepository repo) => _repo = repo;

    private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")?.Value ?? "1");

    // ═══════════════════════════════════════════════════════════════
    // PROVINCIAS (Datos nacionales de RD - no requieren Idempresa)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene todas las provincias de República Dominicana.
    /// </summary>
    [HttpGet("provincias")]
    [AllowAnonymous]  // Las provincias son públicas
    public async Task<ActionResult<IReadOnlyList<ProvinciaDto>>> GetProvincias(CancellationToken ct)
    {
        var result = await _repo.GetProvinciasAsync(ct);
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // MUNICIPIOS (Dependen de provincia)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene los municipios de una provincia específica.
    /// </summary>
    [HttpGet("provincias/{idProvincia:int}/municipios")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<MunicipioDto>>> GetMunicipios(
        int idProvincia,
        CancellationToken ct)
    {
        if (idProvincia <= 0)
            return BadRequest(new { message = "El ID de provincia debe ser mayor a 0." });

        var result = await _repo.GetMunicipiosByProvinciaAsync(idProvincia, ct);
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // SECTORES (Dependen de municipio)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene los sectores de un municipio específico.
    /// </summary>
    [HttpGet("municipios/{idMunicipio:int}/sectores")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<SectorDto>>> GetSectores(
        int idMunicipio,
        CancellationToken ct)
    {
        if (idMunicipio <= 0)
            return BadRequest(new { message = "El ID de municipio debe ser mayor a 0." });

        var result = await _repo.GetSectoresByMunicipioAsync(idMunicipio, ct);
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // RUTAS (Específicas de cada empresa)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene las rutas únicas de clientes de la empresa del usuario.
    /// </summary>
    [HttpGet("rutas")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<RutaDto>>> GetRutas(CancellationToken ct)
    {
        var result = await _repo.GetRutasAsync(GetIdEmpresa(), ct);
        return Ok(result);
    }
}