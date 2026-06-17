using EmmaSystem.Application.DTOs.Categoria;

namespace EmmaSystem.Application.Interfaces;

public interface ICategoriaRepository
{
    Task<IReadOnlyList<CategoriaDto>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<CategoriaDto>> GetForArticuloAsync(CancellationToken ct);
    Task<IReadOnlyList<CategoriaDto>> SearchAsync(string texto, CancellationToken ct);
    Task CreateAsync(CategoriaSaveDto dto, CancellationToken ct);
    Task UpdateAsync(int id, CategoriaSaveDto dto, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}