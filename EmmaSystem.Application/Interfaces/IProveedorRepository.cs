using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmmaSystem.Application.DTOs.Proveedor;
namespace EmmaSystem.Application.Interfaces;
public interface IProveedorRepository
{
    Task<IReadOnlyList<ProveedorDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default);
    Task CreateAsync(ProveedorSaveDto dto, int idEmpresa, int idLogin, CancellationToken ct = default);
    Task UpdateAsync(int idProveedor, ProveedorSaveDto dto, int idLogin, CancellationToken ct = default);
    Task DeleteAsync(int idProveedor, int idLogin, CancellationToken ct = default);
}