using EmmaSystem.Application.DTOs.Ubicacion;

namespace EmmaSystem.Application.Interfaces;

public interface IUbicacionRepository
{
    /// <summary>
    /// Obtiene todas las provincias de RD ordenadas alfabéticamente.
    /// </summary>
    Task<IReadOnlyList<ProvinciaDto>> GetProvinciasAsync(CancellationToken ct = default);

    /// <summary>
    /// Obtiene los municipios de una provincia específica.
    /// </summary>
    Task<IReadOnlyList<MunicipioDto>> GetMunicipiosByProvinciaAsync(int idProvincia, CancellationToken ct = default);

    /// <summary>
    /// Obtiene los sectores de un municipio específico.
    /// </summary>
    Task<IReadOnlyList<SectorDto>> GetSectoresByMunicipioAsync(int idMunicipio, CancellationToken ct = default);

    /// <summary>
    /// Obtiene las rutas únicas de clientes de una empresa.
    /// </summary>
    Task<IReadOnlyList<RutaDto>> GetRutasAsync(int idEmpresa, CancellationToken ct = default);
}