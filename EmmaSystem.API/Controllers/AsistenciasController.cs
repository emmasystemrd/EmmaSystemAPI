using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Asistencia;
using EmmaSystem.Application.Interfaces; // ✅ Solo importamos la interfaz
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/asistencias")]
[Authorize]
public sealed class AsistenciasController : ControllerBase
{
    // ✅ Solo declaramos la interfaz, NO la clase concreta
    private readonly IAsistenciasRepository _repo;

    public AsistenciasController(IAsistenciasRepository repo)
    {
        _repo = repo;
    }

    private int GetIdLogin() => int.Parse(User.FindFirst("Idusuario")?.Value ?? "1");

    /// <summary>
    /// Obtiene la lista de estudiantes para tomar asistencia (o los ya registrados)
    /// </summary>
    [HttpGet("estudiantes")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<List<EstudianteAsistenciaDto>>> GetEstudiantes(
        [FromQuery] int idAsistencia,
        [FromQuery] DateTime fecha,
        [FromQuery] int idCurso,
        [FromQuery] int? idDetalleCurso,
        [FromQuery] int idInstructor,
        CancellationToken ct)
    {
        var result = await _repo.GetEstudiantesParaAsistenciaAsync(idAsistencia, fecha, idCurso, idDetalleCurso, idInstructor, ct);
        return Ok(result);
    }

    /// <summary>
    /// Guarda o actualiza una sesión de asistencia completa
    /// </summary>
    [HttpPost]
    [Permission(Modules.Educacion, Operations.Crear)]
    public async Task<ActionResult> SaveAsistencia([FromBody] AsistenciaSaveDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        dto.Idlogin = GetIdLogin();

        var idAsistencia = await _repo.SaveAsistenciaAsync(dto, ct);
        return Ok(new { idAsistencia, message = "Asistencia guardada correctamente." });
    }

    /// <summary>
    /// Obtiene la matriz de asistencia (Días 1-31) para reportes
    /// </summary>
    [HttpGet("matrix")]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<List<AsistenciaMatrixDto>>> GetMatrix(
        [FromQuery] DateTime fecha1,
        [FromQuery] DateTime fecha2,
        [FromQuery] int idCurso,
        [FromQuery] int? idDetalleCurso,
        [FromQuery] int idInstructor,
        CancellationToken ct)
    {
        var result = await _repo.GetMatrixAsistenciaAsync(fecha1, fecha2, idCurso, idDetalleCurso, idInstructor, ct);
        return Ok(result);
    }
}