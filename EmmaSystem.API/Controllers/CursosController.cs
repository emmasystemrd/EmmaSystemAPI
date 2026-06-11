using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Curso;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/cursos")]
[Authorize]
public sealed class CursosController : ControllerBase
{
    private readonly ICursosRepository _repo;

    public CursosController(ICursosRepository repo) => _repo = repo;

    /// <summary>
    /// Obtiene todos los cursos activos
    /// </summary>
    [HttpGet]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CursoListadoDto>>> GetAll(CancellationToken ct)
    {
        return Ok(await _repo.GetAllAsync(ct));
    }

    /// <summary>
    /// Obtiene un curso por su ID con todos sus detalles
    /// </summary>
    [HttpGet("{id:int}")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<CursoDetalleDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _repo.GetByIdAsync(id, ct);
        if (result is null)
            return NotFound(new { message = $"No se encontró el curso con ID {id}." });
        return Ok(result);
    }

    /// <summary>
    /// Busca un curso por código exacto
    /// </summary>
    [HttpGet("codigo/{codigo}")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<CursoListadoDto>> GetByCodigo(string codigo, CancellationToken ct)
    {
        var result = await _repo.GetByCodigoAsync(codigo, ct);
        if (result is null)
            return NotFound(new { message = $"No se encontró el curso con código {codigo}." });
        return Ok(result);
    }

    /// <summary>
    /// Busca cursos por texto en múltiples campos
    /// </summary>
    [HttpGet("buscar")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CursoListadoDto>>> Search(
        [FromQuery] string texto,
        CancellationToken ct)
    {
        return Ok(await _repo.SearchAsync(texto ?? "", ct));
    }

    /// <summary>
    /// Busca solo cursos activos por texto
    /// </summary>
    [HttpGet("buscar/activos")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CursoListadoDto>>> SearchActivos(
        [FromQuery] string texto,
        CancellationToken ct)
    {
        return Ok(await _repo.SearchActivosAsync(texto ?? "", ct));
    }

    /// <summary>
    /// Obtiene la próxima secuencia disponible para el código del curso
    /// </summary>
    [HttpGet("secuencia")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<int>> GetSecuencia(CancellationToken ct)
    {
        return Ok(await _repo.GetNextSecuenciaAsync(ct));
    }

    /// <summary>
    /// Obtiene los detalles (temario) de un curso
    /// </summary>
    [HttpGet("{id:int}/detalles")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<CursoDetalleItemDto>>> GetDetalles(int id, CancellationToken ct)
    {
        return Ok(await _repo.GetDetallesAsync(id, ct));
    }

    /// <summary>
    /// Crea un nuevo curso con sus detalles
    /// </summary>
    [HttpPost]
    [Permission(Modules.Educacion, Operations.Crear)]
    public async Task<ActionResult<int>> Create([FromBody] CursoSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var idCurso = await _repo.InsertAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = idCurso }, idCurso);
    }

    /// <summary>
    /// Actualiza un curso existente y sus detalles
    /// </summary>
    [HttpPut("{id:int}")]
    [Permission(Modules.Educacion, Operations.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] CursoSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _repo.UpdateAsync(id, dto, ct);
        return Ok(new { message = "Curso actualizado correctamente." });
    }

    /// <summary>
    /// Elimina un curso (cambio de estado a 'E')
    /// </summary>
    [HttpDelete("{id:int}")]
    [Permission(Modules.Educacion, Operations.Eliminar)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
        return Ok(new { message = "Curso eliminado correctamente." });
    }
}