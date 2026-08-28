using EmmaSystem.Application.DTOs.Estudiante;

namespace EmmaSystem.Application.Interfaces;

public interface IEstudiantesRepository
{
    Task<IReadOnlyList<EstudianteListadoDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EstudianteListadoDto>> SearchAsync(string texto, CancellationToken ct = default);
    Task<EstudianteDetalleDto?> GetByIdAsync(int idEstudiante, CancellationToken ct = default);
    Task<int> InsertAsync(EstudianteSaveDto dto, CancellationToken ct = default);
    Task UpdateAsync(int idEstudiante, EstudianteSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(int idEstudiante, CancellationToken ct = default);
    Task<byte[]?> GetFotoAsync(int idEstudiante, CancellationToken ct = default);
    // ═══════════════════════════════════════════════════════════════
    // BÚSQUEDA DE ESTUDIANTES ACTIVOS (para selector de Inscripción)
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Busca estudiantes activos por nombre, código o cédula.
    /// </summary>
    Task<IReadOnlyList<EstudianteBusquedaDto>> BuscarEstudiantesActivosAsync(string textoBuscar, CancellationToken ct = default);
}