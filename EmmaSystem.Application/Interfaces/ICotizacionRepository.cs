using EmmaSystem.Application.DTOs.Cotizacion;

namespace EmmaSystem.Application.Interfaces;

public interface ICotizacionRepository
{
    Task<IReadOnlyList<CotizacionDto>> GetAllAsync(int idEmpresa, string tipo, CancellationToken ct = default);
    Task<IReadOnlyList<CotizacionDto>> SearchAsync(string texto, string tipo, DateTime fecha1, DateTime fecha2, string proceso, int idEmpresa, CancellationToken ct = default);

    Task<CotizacionDto?> GetByIdWithClientAsync(int idCotizacion, CancellationToken ct = default);
    Task<int> CreateAsync(CotizacionSaveDto dto, int idEmpresa, CancellationToken ct = default);
    Task UpdateAsync(int idCotizacion, CotizacionSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(int idCotizacion, CancellationToken ct = default);
    Task CloseAsync(int idCotizacion, CancellationToken ct = default);

    Task<IReadOnlyList<CotizacionDetalleDto>> GetDetailsAsync(int idCotizacion, CancellationToken ct = default);
    Task AddDetailAsync(CotizacionDetalleSaveDto dto, CancellationToken ct = default);
    Task UpdateDetailAsync(int idDetalle, CotizacionDetalleSaveDto dto, CancellationToken ct = default);
    Task DeleteDetailAsync(int idDetalle, CancellationToken ct = default);
    Task ClearDetailsAsync(int idCotizacion, CancellationToken ct = default);

    Task<IReadOnlyList<VendedorDto>> GetVendedoresAsync(int idEmpresa, CancellationToken ct = default);
    Task<CotizacionImpresionDto?> GetPrintDataAsync(
    string noCotizacion,
    string tipo,
    int idEmpresa,
    CancellationToken ct = default);
    Task<string> GetNextSequenceAsync(string tipo, int idEmpresa, CancellationToken ct = default);

}