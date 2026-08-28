using EmmaSystem.API.Filters;
using EmmaSystem.Application.Constants;
using EmmaSystem.Application.DTOs.Reportes;
using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmmaSystem.API.Controllers;

[ApiController]
[Route("api/reportes/estudiantes")]
[Authorize]
public sealed class ReportesEstudiantesController : ControllerBase
{
    private readonly IReportesEstudiantesRepository _repo;

    public ReportesEstudiantesController(IReportesEstudiantesRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Reporte de estudiantes con filtros por texto, estado del estudiante, curso y estado de inscripción.
    /// </summary>
    [HttpGet]
    [Permission(Modules.Educacion, Operations.Ver)]
    public async Task<ActionResult<IReadOnlyList<EstudianteReporteDto>>> GetReporte(
        [FromQuery] string texto = "",
        [FromQuery] string? estadoEstudiante = null,
        [FromQuery] int? idCurso = null,
        [FromQuery] string? estadoInscripcion = null,
        CancellationToken ct = default)
    {
        var result = await _repo.GetReporteEstudiantesAsync(texto, estadoEstudiante, idCurso, estadoInscripcion, ct);
        return Ok(result);
    }
}