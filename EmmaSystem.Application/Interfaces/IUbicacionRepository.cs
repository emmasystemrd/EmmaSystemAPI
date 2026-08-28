using EmmaSystem.Application.DTOs.Ubicacion;

namespace EmmaSystem.Application.Interfaces;

public interface IUbicacionRepository
{
    Task<IReadOnlyList<ProvinciaDto>> GetProvinciasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MunicipioDto>> GetMunicipiosByProvinciaAsync(int idProvincia, CancellationToken ct = default);
    Task<IReadOnlyList<SectorDto>> GetSectoresByMunicipioAsync(int idMunicipio, CancellationToken ct = default);
    Task<IReadOnlyList<RutaDto>> GetRutasAsync(int idEmpresa, CancellationToken ct = default);
    Task<IReadOnlyList<NacionalidadDto>> GetNacionalidadesAsync(CancellationToken ct = default); // ✅ NUEVO
}