using EmmaSystem.Application.DTOs.Medida;

namespace EmmaSystem.Application.Interfaces;

public interface IMedidaRepository
{
    Task<IReadOnlyList<MedidaDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default);
    Task<IReadOnlyList<MedidaArticuloDto>> GetForArticuloAsync(int idEmpresa, CancellationToken ct = default);
    Task<IReadOnlyList<MedidaDto>> SearchAsync(string texto, int idEmpresa, CancellationToken ct = default);
    Task<MedidaDto?> GetByIdAsync(int idMedida, CancellationToken ct = default);
    Task CreateAsync(MedidaSaveDto dto, int idEmpresa, CancellationToken ct = default);
    Task UpdateAsync(int idMedida, MedidaSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(int idMedida, CancellationToken ct = default);
    
    Task<IReadOnlyList<MedidaDetalleProductoDto>> GetDetallesByProductoAsync(
        int idProducto,
        CancellationToken ct = default);

}