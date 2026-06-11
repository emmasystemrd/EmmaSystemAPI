using EmmaSystem.Application.DTOs.Articulo;

namespace EmmaSystem.Application.Interfaces;

public interface IArticuloRepository
{
    Task<IReadOnlyList<ArticuloDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default);
    Task<IReadOnlyList<ArticuloDto>> SearchAsync(string texto, int idEmpresa, CancellationToken ct = default);
    Task<int> CreateAsync(ArticuloSaveDto dto, int idEmpresa, int idLogin, CancellationToken ct = default);
    Task UpdateAsync(int idArticulo, ArticuloSaveDto dto, int idLogin, CancellationToken ct = default);
    Task DeleteAsync(int idArticulo, int idLogin, CancellationToken ct = default);
    Task<IReadOnlyList<ArticuloVentaDto>> SearchForSalesAsync(string textoBuscar, int idEmpresa, CancellationToken ct = default);
    Task<IReadOnlyList<DetalleProductoPrecioDto>> GetDetallePreciosAsync(
    int idArticulo,
    int idMedida,
    string nombre,
    CancellationToken ct = default);
}