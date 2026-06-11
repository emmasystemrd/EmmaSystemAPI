using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Cobro;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/cobros")]
[Authorize]
public sealed class CobrosController : ControllerBase
{
    private readonly ICobrosRepository _repo;

    public CobrosController(ICobrosRepository repo) => _repo = repo;

    private int GetIdLogin() => int.Parse(User.FindFirst("Idusuario")?.Value ?? "1");
    private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")?.Value ?? "1");

    // ═══ COBROS NORMALES ═══

    [HttpGet]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CobroListadoDto>>> GetAll(CancellationToken ct)
    {
        return Ok(await _repo.GetAllAsync(GetIdEmpresa(), ct));
    }

    [HttpGet("buscar")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CobroListadoDto>>> Search(
        [FromQuery] DateTime? fecha1,
        [FromQuery] DateTime? fecha2,
        [FromQuery] bool isFecha = false,
        [FromQuery] string texto = "",
        [FromQuery] string columna = "cl.Razon_Social",
        [FromQuery] int adjunto = 0,
        CancellationToken ct = default)
    {
        return Ok(await _repo.SearchAsync(GetIdEmpresa(), fecha1, fecha2, isFecha, texto, columna, adjunto, ct));
    }

    [HttpGet("{id:int}")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<CobroDetalleDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _repo.GetByIdAsync(id, GetIdEmpresa(), ct);
        if (result is null) return NotFound(new { message = "Cobro no encontrado." });
        return Ok(result);
    }

    [HttpGet("codigo/{codigo}")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<CobroDetalleDto>> GetByCodigo(string codigo, CancellationToken ct)
    {
        var result = await _repo.GetByCodigoAsync(codigo, GetIdEmpresa(), ct);
        if (result is null) return NotFound(new { message = "Cobro no encontrado." });
        return Ok(result);
    }

    [HttpGet("{id:int}/pdf")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<IActionResult> GetPdf(int id, CancellationToken ct)
    {
        var pdf = await _repo.GetPdfAsync(id, ct);
        if (pdf == null || pdf.Length == 0)
            return NotFound(new { message = "El cobro no tiene PDF adjunto." });
        return Ok(new { pdf = Convert.ToBase64String(pdf) });
    }

    [HttpPost]
    [Permission(Modules.Cliente, Operations.Crear)]
    public async Task<ActionResult<int>> Create([FromBody] CobroSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var id = await _repo.InsertAsync(dto, GetIdLogin(), GetIdEmpresa(), ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:int}")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] CobroSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _repo.UpdateAsync(id, dto, GetIdLogin(), ct);
        return Ok(new { message = "Cobro actualizado correctamente." });
    }

    [HttpDelete("{id:int}")]
    [Permission(Modules.Cliente, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, GetIdLogin(), ct);
        return Ok(new { message = "Cobro eliminado correctamente." });
    }

    // ═══ AVANCES DE CLIENTE ═══

    [HttpGet("avances")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CobroListadoDto>>> GetAllAvances(CancellationToken ct)
    {
        return Ok(await _repo.GetAllAvancesAsync(GetIdEmpresa(), ct));
    }

    [HttpGet("avances/buscar")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CobroListadoDto>>> SearchAvances(
        [FromQuery] DateTime? fecha1,
        [FromQuery] DateTime? fecha2,
        [FromQuery] bool isFecha = false,
        [FromQuery] string texto = "",
        [FromQuery] string columna = "cl.Razon_Social",
        [FromQuery] int adjunto = 0,
        CancellationToken ct = default)
    {
        return Ok(await _repo.SearchAvancesAsync(GetIdEmpresa(), fecha1, fecha2, isFecha, texto, columna, adjunto, ct));
    }

    [HttpPost("avances")]
    [Permission(Modules.Cliente, Operations.Crear)]
    public async Task<ActionResult<int>> CreateAvance([FromBody] AvanceClienteSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var id = await _repo.InsertAvanceAsync(dto, GetIdLogin(), GetIdEmpresa(), ct);
        return Ok(new { id, message = "Avance de cliente registrado correctamente." });
    }

    [HttpPut("avances/{id:int}")]
    [Permission(Modules.Cliente, Operations.Editar)]
    public async Task<IActionResult> UpdateAvance(int id, [FromBody] AvanceClienteSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _repo.UpdateAvanceAsync(id, dto, GetIdLogin(), ct);
        return Ok(new { message = "Avance actualizado correctamente." });
    }

    // ═══ DETALLES ═══

    [HttpGet("{idCobro:int}/detalles")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<DetalleCobroDto>>> GetDetalles(
        int idCobro,
        [FromQuery] int idCliente,
        [FromQuery] DateTime fecha,
        CancellationToken ct)
    {
        return Ok(await _repo.GetDetallesAsync(idCobro, idCliente, fecha, GetIdEmpresa(), ct));
    }

    [HttpGet("documento")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<int>> GetIdDocumento(
        [FromQuery] string tipo,
        [FromQuery] string noFactura,
        CancellationToken ct)
    {
        var id = await _repo.GetIdDocumentoAsync(GetIdEmpresa(), tipo, noFactura, ct);
        return Ok(id);
    }

    // ═══ REPORTE ═══

    [HttpGet("reporte")]
    [Permission(Modules.Cliente, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CobroReporteDto>>> GetReporte(
        [FromQuery] DateTime fecha1,
        [FromQuery] DateTime fecha2,
        [FromQuery] int idUsuario = 0,
        CancellationToken ct = default)
    {
        return Ok(await _repo.GetReporteAsync(GetIdEmpresa(), fecha1, fecha2, idUsuario, ct));
    }
}