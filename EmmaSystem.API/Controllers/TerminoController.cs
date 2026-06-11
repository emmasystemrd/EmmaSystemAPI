using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Termino;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/termino")]
[Authorize]
public sealed class TerminoController : ControllerBase
{
    private readonly ITerminoRepository _repo;

    public TerminoController(ITerminoRepository repo) => _repo = repo;

    private int GetIdEmpresa() => int.Parse(User.FindFirst("Idempresa")!.Value);

    [HttpGet]
    [Permission(Modules.System, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<TerminoDto>>> GetAll(CancellationToken ct)
    {
        var result = await _repo.GetAllAsync(GetIdEmpresa(), ct);
        return Ok(result);
    }

    [HttpGet("buscar")]
    [Permission(Modules.System, Operations.Consultar)]
    public async Task<ActionResult<IReadOnlyList<TerminoDto>>> Search([FromQuery] string texto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return BadRequest("El texto de búsqueda es requerido.");

        var result = await _repo.SearchAsync(texto.Trim(), GetIdEmpresa(), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Permission(Modules.System, Operations.Ver)]
    public async Task<ActionResult<TerminoDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _repo.GetByIdAsync(id, GetIdEmpresa(), ct);
        if (result is null)
            return NotFound(new { message = $"No se encontró el término con ID {id}." });

        return Ok(result);
    }

    [HttpPost]
    [Permission(Modules.System, Operations.Crear)]
    public async Task<IActionResult> Create([FromBody] TerminoSaveDto dto, CancellationToken ct)
    {
        if (dto.Tipo != "C" && dto.Tipo != "P")
            return BadRequest("El campo 'Tipo' debe ser 'C' (Cliente) o 'P' (Proveedor).");

        await _repo.CreateAsync(dto, GetIdEmpresa(), ct);
        return Ok(new { message = "Término de pago creado exitosamente." });
    }

    [HttpPut("{id:int}")]
    [Permission(Modules.System, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] TerminoSaveDto dto, CancellationToken ct)
    {
        if (dto.Tipo != "C" && dto.Tipo != "P")
            return BadRequest("El campo 'Tipo' debe ser 'C' (Cliente) o 'P' (Proveedor).");

        await _repo.UpdateAsync(id, dto, ct);
        return Ok(new { message = "Término de pago actualizado exitosamente." });
    }

    [HttpDelete("{id:int}")]
    [Permission(Modules.System, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
        return Ok(new { message = "Término de pago eliminado exitosamente." });
    }
}