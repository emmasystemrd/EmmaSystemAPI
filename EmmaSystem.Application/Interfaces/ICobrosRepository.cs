using EmmaSystem.Application.DTOs.Cobro;

namespace EmmaSystem.Application.Interfaces;

public interface ICobrosRepository
{
    // Cobros normales
    Task<IReadOnlyList<CobroListadoDto>> GetAllAsync(int idEmpresa, CancellationToken ct = default);
    Task<IReadOnlyList<CobroListadoDto>> SearchAsync(int idEmpresa, DateTime? fecha1, DateTime? fecha2, bool isFecha, string texto, string columna, int adjunto, CancellationToken ct = default);
    Task<CobroDetalleDto?> GetByIdAsync(int idCobro, int idEmpresa, CancellationToken ct = default);
    Task<CobroDetalleDto?> GetByCodigoAsync(string codigo, int idEmpresa, CancellationToken ct = default);
    Task<int> InsertAsync(CobroSaveDto dto, int idLogin, int idEmpresa, CancellationToken ct = default);
    Task UpdateAsync(int idCobro, CobroSaveDto dto, int idLogin, CancellationToken ct = default);
    Task DeleteAsync(int idCobro, int idLogin, CancellationToken ct = default);

    // Avances de cliente
    Task<IReadOnlyList<CobroListadoDto>> GetAllAvancesAsync(int idEmpresa, CancellationToken ct = default);
    Task<IReadOnlyList<CobroListadoDto>> SearchAvancesAsync(int idEmpresa, DateTime? fecha1, DateTime? fecha2, bool isFecha, string texto, string columna, int adjunto, CancellationToken ct = default);
    Task<int> InsertAvanceAsync(AvanceClienteSaveDto dto, int idLogin, int idEmpresa, CancellationToken ct = default);
    Task UpdateAvanceAsync(int idCobro, AvanceClienteSaveDto dto, int idLogin, CancellationToken ct = default);

    // Detalles
    Task<IReadOnlyList<DetalleCobroDto>> GetDetallesAsync(int idCobro, int idCliente, DateTime fecha, int idEmpresa, CancellationToken ct = default);
    Task<int> GetIdDocumentoAsync(int idEmpresa, string tipo, string noFactura, CancellationToken ct = default);

    // Reporte
    Task<IReadOnlyList<CobroReporteDto>> GetReporteAsync(int idEmpresa, DateTime fecha1, DateTime fecha2, int idUsuario, CancellationToken ct = default);

    // PDF
    Task<byte[]?> GetPdfAsync(int idCobro, CancellationToken ct = default);
}