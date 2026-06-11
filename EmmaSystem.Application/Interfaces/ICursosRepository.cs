using EmmaSystem.Application.DTOs.Curso;

namespace EmmaSystem.Application.Interfaces;

public interface ICursosRepository
{
    Task<IReadOnlyList<CursoListadoDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CursoListadoDto>> SearchAsync(string texto, CancellationToken ct = default);
    Task<IReadOnlyList<CursoListadoDto>> SearchActivosAsync(string texto, CancellationToken ct = default);
    Task<CursoDetalleDto?> GetByIdAsync(int idCurso, CancellationToken ct = default);
    Task<CursoListadoDto?> GetByCodigoAsync(string codigo, CancellationToken ct = default);
    Task<int> InsertAsync(CursoSaveDto curso, CancellationToken ct = default);
    Task UpdateAsync(int idCurso, CursoSaveDto curso, CancellationToken ct = default);
    Task DeleteAsync(int idCurso, CancellationToken ct = default);
    Task<int> GetNextSecuenciaAsync(CancellationToken ct = default);

    // Detalles
    Task<IReadOnlyList<CursoDetalleItemDto>> GetDetallesAsync(int idCurso, CancellationToken ct = default);
}