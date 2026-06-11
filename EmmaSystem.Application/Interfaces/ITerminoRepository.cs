using EmmaSystem.Application.DTOs.Termino;

namespace EmmaSystem.Application.Interfaces;

public interface ITerminoRepository
{
    Task<IReadOnlyList<TerminoDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default);
    Task<IReadOnlyList<TerminoDto>> SearchAsync(string texto, int idEmpresa, CancellationToken ct = default);
    Task<TerminoDto?> GetByIdAsync(int idTermino, int idEmpresa, CancellationToken ct = default);
    Task CreateAsync(TerminoSaveDto dto, int idEmpresa, CancellationToken ct = default);
    Task UpdateAsync(int idTermino, TerminoSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(int idTermino, CancellationToken ct = default);
}