using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Estudiante;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/calificaciones")]
[Authorize]
public sealed class CalificacionesController : ControllerBase
{
    private readonly ICalificacionesRepository _repo;

    public CalificacionesController(ICalificacionesRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Obtiene las INSCRIPCIONES de estudiantes en un curso específico, con su estado de calificación.
    /// </summary>
    [HttpGet("curso/{idCurso:int}")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<EstudianteCalificacionDto>>> GetEstudiantesByCurso(
        int idCurso, CancellationToken ct)
    {
        var result = await _repo.GetEstudiantesByCursoAsync(idCurso, ct);
        return Ok(result);
    }

    /// <summary>
    /// Actualiza el estado de una INSCRIPCIÓN (Desertor, Retiro Formal, Aprobado Certificado).
    /// NO modifica el estado global del estudiante.
    /// </summary>
    [HttpPut("estado")]
    [Permission(Modules.Educacion, Operations.Editar)]
    public async Task<IActionResult> ActualizarEstadoInscripcion(
        [FromBody] ActualizarEstadoInscripcionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _repo.ActualizarEstadoInscripcionAsync(dto.IdInscripcion, dto.NuevoEstado, ct);
        return Ok(new { message = "Estado de la inscripción actualizado correctamente." });
    }
}