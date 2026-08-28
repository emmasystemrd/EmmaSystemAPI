using Dapper;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;

namespace EmmaSystem.Infrastructure.Repositories;

public class VersionService : IVersionService
{
    private readonly SqlConnectionFactory _centralFactory;

    public VersionService(SqlConnectionFactory centralFactory)
    {
        _centralFactory = centralFactory;
    }

    public async Task<VersionResponseDto> VerificarActualizacionAsync(string versionActual)
    {
        const string sql = @"
            SELECT TOP 1 Version, URL_Instalador, URL_ScriptSQL, Descripcion, EsObligatorio,
                   SHA256_Zip, VersionDB
            FROM Versiones_Sistema
            ORDER BY Version DESC";

        using var connection = _centralFactory.CreateConnection();
        var version = await connection.QueryFirstOrDefaultAsync<dynamic>(sql);

        if (version == null)
            return new VersionResponseDto { HayActualizacion = false };

        bool hayActualizacion = EsVersionNueva(version.Version, versionActual);

        return new VersionResponseDto
        {
            HayActualizacion = hayActualizacion,
            VersionNueva = version.Version,
            URL_Instalador = version.URL_Instalador,
            URL_ScriptSQL = version.URL_ScriptSQL,
            Descripcion = version.Descripcion,
            EsObligatorio = version.EsObligatorio,
            SHA256_Zip = version.SHA256_Zip,
            VersionDB = version.VersionDB
        };
    }

    public async Task<VersionInfoDto> ObtenerUltimaVersionAsync()
    {
        const string sql = @"
            SELECT TOP 1 Version, FechaLanzamiento, Descripcion
            FROM Versiones_Sistema
            ORDER BY Version DESC";

        using var connection = _centralFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<VersionInfoDto>(sql)
               ?? new VersionInfoDto();
    }

    //private bool EsVersionNueva(string versionNueva, string versionActual)
    //{
    //    try
    //    {
    //        var nueva = new Version(versionNueva);
    //        var actual = new Version(versionActual);
    //        return nueva > actual;
    //    }
    //    catch
    //    {
    //        return versionNueva != versionActual;
    //    }
    //}
    private bool EsVersionNueva(string versionNueva, string versionActual)
    {
        if (Version.TryParse(versionNueva, out var nueva) && Version.TryParse(versionActual, out var actual))
        {
            return nueva > actual; // ✅ Solo notificar si es estrictamente mayor
        }
        return false;
    }
}