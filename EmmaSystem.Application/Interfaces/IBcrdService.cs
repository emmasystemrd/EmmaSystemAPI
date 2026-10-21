using EmmaSystem.Application.DTOs.BCRD;

namespace EmmaSystem.Application.Interfaces;

public interface IBcrdService
{
    Task<List<TasaCambioDto>> ObtenerTasasAsync(DateTime fechaDesde, DateTime fechaHasta);
}