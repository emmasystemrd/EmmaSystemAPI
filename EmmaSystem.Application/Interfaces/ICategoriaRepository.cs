using EmmaSystem.Application.DTOs.Categoria;

namespace EmmaSystem.Application.Interfaces;

public interface ICategoriaRepository
{
    Task<IReadOnlyList<CategoriaDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default);
    Task<IReadOnlyList<CategoriaDto>> GetForArticuloAsync(int idEmpresa, CancellationToken ct = default);
    Task<IReadOnlyList<CategoriaDto>> SearchAsync(string texto, int idEmpresa, CancellationToken ct = default);
    Task CreateAsync(CategoriaSaveDto dto, int idEmpresa, CancellationToken ct = default);
    Task UpdateAsync(int idCategoria, CategoriaSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(int idCategoria, CancellationToken ct = default);
}