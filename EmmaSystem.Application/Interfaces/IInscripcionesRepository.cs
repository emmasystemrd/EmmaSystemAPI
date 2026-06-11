using EmmaSystem.Application.DTOs.Inscripcion;
using EmmaSystem.Application.DTOs.Estudiante; // Para reutilizar el DTO de estudiante

namespace EmmaSystem.Application.Interfaces;

public interface IInscripcionesRepository
{
    Task<IReadOnlyList<InscripcionListadoDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InscripcionListadoDto>> SearchAsync(DateTime? fecha1, DateTime? fecha2, bool isFecha, string texto, string columna, CancellationToken ct = default);
    Task<InscripcionDetalleDto?> GetByIdAsync(int idInscripcion, CancellationToken ct = default);
    Task<InscripcionImpresionDto?> GetPrintDataAsync(int idInscripcion, CancellationToken ct = default);
    Task<EstudianteDetalleDto?> GetEstudianteByCodigoAsync(string textoBuscar, CancellationToken ct = default);
    Task<int> InsertAsync(InscripcionSaveDto dto, CancellationToken ct = default);
    Task UpdateAsync(int idInscripcion, InscripcionSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(int idInscripcion, CancellationToken ct = default);
}