using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Configuracion;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/encf")]
[Authorize]
public sealed class EnCFController : ControllerBase
{
    private readonly IEnCFRepository _repo;

    public EnCFController(IEnCFRepository repo) => _repo = repo;

    [HttpGet]
    [Permission(Modules.System, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<ENcfDto>>> GetAll(CancellationToken ct)
    {
        return Ok(await _repo.GetAllAsync(ct));
    }

    [HttpGet("buscar")]
    [Permission(Modules.System, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<ENcfDto>>> Search([FromQuery] string tipo, CancellationToken ct)
    {
        return Ok(await _repo.SearchAsync(tipo, ct));
    }

    [HttpPost]
    [Permission(Modules.System, Operations.Crear)]
    public async Task<IActionResult> Insert([FromBody] ENcfDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _repo.InsertAsync(dto, ct);
        return Ok(new { message = "Secuencia eNCF registrada correctamente." });
    }

    [HttpPut("{id}")]
    [Permission(Modules.System, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] ENcfDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _repo.UpdateAsync(id, dto, ct);
        return Ok(new { message = "Secuencia eNCF actualizada correctamente." });
    }

    [HttpDelete("{id}")]
    [Permission(Modules.System, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
        return Ok(new { message = "Secuencia eNCF eliminada correctamente." });
    }
}