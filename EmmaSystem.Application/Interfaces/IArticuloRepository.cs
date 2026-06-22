using EmmaSystem.Application.DTOs.Articulo;

namespace EmmaSystem.Application.Interfaces;

public interface IArticuloRepository
{
    Task<IReadOnlyList<ArticuloDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticuloDto>> SearchAsync(string texto, CancellationToken ct = default);
    Task<int> CreateAsync(ArticuloSaveDto dto, int idLogin, CancellationToken ct = default);
    Task UpdateAsync(int idArticulo, ArticuloSaveDto dto, int idLogin, CancellationToken ct = default);
    Task DeleteAsync(int idArticulo, int idLogin, CancellationToken ct = default);
    Task<IReadOnlyList<ArticuloVentaDto>> SearchForSalesAsync(string textoBuscar, CancellationToken ct = default);
    Task<IReadOnlyList<DetalleProductoPrecioDto>> GetDetallePreciosAsync(
        int idArticulo,
        int idMedida,
        string nombre,
        CancellationToken ct = default);
    Task<ArticuloDto?> GetByIdAsync(int idArticulo, CancellationToken ct = default);
    Task<int> GetSecuenciaAsync(string tipo, CancellationToken ct = default);
}