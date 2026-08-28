using EmmaSystem.Application.DTOs.ECF;

namespace EmmaSystem.Application.Interfaces;

public interface IEcfRecibidoRepository
{
    Task<List<EcfRecibidoDto>> ObtenerPorRncCompradorAsync(string rncComprador, CancellationToken ct = default);
    Task<EcfRecibidoDto?> ObtenerPorIdAsync(long id, CancellationToken ct = default);
}