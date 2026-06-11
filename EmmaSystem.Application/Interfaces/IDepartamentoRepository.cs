using EmmaSystem.Application.DTOs.Departamento;

namespace EmmaSystem.Application.Interfaces;

public interface IDepartamentoRepository
{
    Task<IReadOnlyList<DepartamentoDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default);
    Task<IReadOnlyList<DepartamentoDto>> SearchAsync(string textoBuscar, int idEmpresa, CancellationToken ct = default);
    Task CreateAsync(DepartamentoCreateDto dto, int idEmpresa, CancellationToken ct = default);
    Task UpdateAsync(int idDepartamento, DepartamentoUpdateDto dto, CancellationToken ct = default);
    Task DeleteAsync(int idDepartamento, CancellationToken ct = default);
}