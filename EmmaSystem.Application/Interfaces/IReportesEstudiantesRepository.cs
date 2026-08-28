using EmmaSystem.Application.DTOs.Reportes;

namespace EmmaSystem.Application.Interfaces;

public interface IReportesEstudiantesRepository
{
    Task<IReadOnlyList<EstudianteReporteDto>> GetReporteEstudiantesAsync(
        string textoBuscar, string? estadoEstudiante, int? idCurso, string? estadoInscripcion,
        CancellationToken ct = default);
}