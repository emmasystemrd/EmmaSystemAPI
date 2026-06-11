using EmmaSystem.Application.DTOs.Configuracion;

namespace EmmaSystem.Application.Interfaces;

public interface IEnCFRepository
{
    Task<IReadOnlyList<ENcfDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ENcfDto>> SearchAsync(string tipo, CancellationToken ct = default);
    Task<int> InsertAsync(ENcfDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, ENcfDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}