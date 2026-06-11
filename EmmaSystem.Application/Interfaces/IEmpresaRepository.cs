using EmmaSystem.Application.DTOs.Empresa;

namespace EmmaSystem.Application.Interfaces;

public interface IEmpresaRepository
{
    Task<EmpresaDto?> GetByIdAsync(int idEmpresa, CancellationToken ct = default);
    Task UpdateAsync(EmpresaDto dto, byte[]? logoBytes, CancellationToken ct = default);
}