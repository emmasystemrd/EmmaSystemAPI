using EmmaSystem.Application.DTOs.Estudiante;

namespace EmmaSystem.Application.Interfaces;

public interface ICalificacionesRepository
{
    Task<IReadOnlyList<EstudianteCalificacionDto>> GetEstudiantesByCursoAsync(
        int idCurso, CancellationToken ct = default);

    // ✅ CAMBIADO: Ahora actualiza la inscripción, no el estudiante
    Task ActualizarEstadoInscripcionAsync(
        int idInscripcion, string nuevoEstado, CancellationToken ct = default);
}