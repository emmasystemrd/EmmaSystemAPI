using Dapper;
using EmmaSystem.Application.DTOs.Ubicacion;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public sealed class UbicacionRepository : IUbicacionRepository
{
    private readonly SqlConnectionFactory _factory;

    public UbicacionRepository(SqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<ProvinciaDto>> GetProvinciasAsync(CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        const string sql = @"
            SELECT Idprovincia, UPPER(Provincia) AS Provincia 
            FROM Provincia 
            ORDER BY Provincia";

        var result = await conn.QueryAsync<ProvinciaDto>(
            new CommandDefinition(sql, cancellationToken: ct));

        return result.AsList();
    }

    public async Task<IReadOnlyList<MunicipioDto>> GetMunicipiosByProvinciaAsync(int idProvincia, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        const string sql = @"
            SELECT Idmunicipio, UPPER(Municipio) AS Municipio 
            FROM Municipio 
            WHERE Idprovincia = @Idprovincia 
            ORDER BY Municipio";

        var result = await conn.QueryAsync<MunicipioDto>(
            new CommandDefinition(sql, new { Idprovincia = idProvincia }, cancellationToken: ct));

        return result.AsList();
    }

    public async Task<IReadOnlyList<SectorDto>> GetSectoresByMunicipioAsync(int idMunicipio, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        const string sql = @"
            SELECT Idsector, UPPER(Sector) AS Sector 
            FROM Sector 
            WHERE Idmunicipio = @Idmunicipio 
            ORDER BY Sector";

        var result = await conn.QueryAsync<SectorDto>(
            new CommandDefinition(sql, new { Idmunicipio = idMunicipio }, cancellationToken: ct));

        return result.AsList();
    }

    public async Task<IReadOnlyList<RutaDto>> GetRutasAsync(int idEmpresa, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();

        const string sql = @"
            SELECT DISTINCT Ruta 
            FROM Cliente1 
            WHERE Ruta != '' 
              AND Idempresa = @Idempresa 
            ORDER BY Ruta";

        var result = await conn.QueryAsync<RutaDto>(
            new CommandDefinition(sql, new { Idempresa = idEmpresa }, cancellationToken: ct));

        return result.AsList();
    }
}