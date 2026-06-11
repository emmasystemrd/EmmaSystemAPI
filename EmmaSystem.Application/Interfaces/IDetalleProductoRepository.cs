using EmmaSystem.Application.DTOs.Articulo;

namespace EmmaSystem.Application.Interfaces;

public interface IDetalleProductoRepository
{
    Task<IReadOnlyList<DetalleProductoDto>> GetByIdArticuloAsync(int idArticulo, CancellationToken ct = default);
    Task CreateAsync(DetalleProductoSaveDto dto, CancellationToken ct = default);
    Task UpdateAsync(int idDetalle, DetalleProductoSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(int idDetalle, CancellationToken ct = default);
}