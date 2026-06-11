using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Estudiante;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/estudiantes")]
[Authorize]
public sealed class EstudiantesController : ControllerBase
{
    private readonly IEstudiantesRepository _repo;

    public EstudiantesController(IEstudiantesRepository repo) => _repo = repo;

    /// <summary>
    /// Obtiene los últimos 100 estudiantes activos
    /// </summary>
    [HttpGet]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<EstudianteListadoDto>>> GetAll(CancellationToken ct)
    {
        return Ok(await _repo.GetAllAsync(ct));
    }

    /// <summary>
    /// Obtiene un estudiante por su ID con todos sus detalles
    /// </summary>
    [HttpGet("{id:int}")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<EstudianteDetalleDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _repo.GetByIdAsync(id, ct);
        if (result is null)
            return NotFound(new { message = $"No se encontró el estudiante con ID {id}." });
        return Ok(result);
    }

    /// <summary>
    /// Obtiene la foto del estudiante en base64
    /// </summary>
    [HttpGet("{id:int}/foto")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<IActionResult> GetFoto(int id, CancellationToken ct)
    {
        var foto = await _repo.GetFotoAsync(id, ct);
        if (foto == null || foto.Length == 0)
            return NotFound(new { message = "El estudiante no tiene foto." });
        return Ok(new { foto = Convert.ToBase64String(foto) });
    }

    /// <summary>
    /// Busca estudiantes por texto en múltiples campos
    /// </summary>
    [HttpGet("buscar")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<EstudianteListadoDto>>> Search(
        [FromQuery] string texto,
        CancellationToken ct)
    {
        return Ok(await _repo.SearchAsync(texto ?? "", ct));
    }

    /// <summary>
    /// Crea un nuevo estudiante
    /// </summary>
    [HttpPost]
    [Permission(Modules.Educacion, Operations.Crear)]
    public async Task<ActionResult<int>> Create([FromBody] EstudianteSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var id = await _repo.InsertAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = id }, id);
    }

    /// <summary>
    /// Actualiza un estudiante existente
    /// </summary>
    [HttpPut("{id:int}")]
    [Permission(Modules.Educacion, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] EstudianteSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _repo.UpdateAsync(id, dto, ct);
        return Ok(new { message = "Estudiante actualizado correctamente." });
    }

    /// <summary>
    /// Elimina un estudiante (cambio de estado a 'E')
    /// </summary>
    [HttpDelete("{id:int}")]
    [Permission(Modules.Educacion, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
        return Ok(new { message = "Estudiante eliminado correctamente." });
    }
}