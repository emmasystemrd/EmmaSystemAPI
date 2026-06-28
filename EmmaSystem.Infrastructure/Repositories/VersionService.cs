using Dapper;
using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace EmmaSystem.Infrastructure.Services
{
    public class VersionService : IVersionService
    {
        private readonly SqlConnectionFactory _centralFactory;

        public VersionService(SqlConnectionFactory centralFactory)
        {
            _centralFactory = centralFactory ?? throw new ArgumentNullException(nameof(centralFactory));
        }

        public async Task<VersionResponseDto> VerificarActualizacionAsync(string versionActual)
        {
            using var connection = _centralFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT TOP 1 Version, URL_Instalador, URL_ScriptSQL, Descripcion, EsObligatorio
                FROM Versiones_Sistema
                WHERE Activo = 1
                ORDER BY FechaLanzamiento DESC";

            var version = await connection.QueryFirstOrDefaultAsync<dynamic>(sql);

            if (version == null)
            {
                return new VersionResponseDto { HayActualizacion = false };
            }

            bool hayActualizacion = EsVersionNueva(version.Version, versionActual);

            return new VersionResponseDto
            {
                HayActualizacion = hayActualizacion,
                VersionNueva = version.Version,
                URL_Instalador = version.URL_Instalador,
                URL_ScriptSQL = version.URL_ScriptSQL,
                Descripcion = version.Descripcion,
                EsObligatorio = version.EsObligatorio
            };
        }

        public async Task<VersionInfoDto> ObtenerUltimaVersionAsync()
        {
            using var connection = _centralFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
        SELECT TOP 1 Version, FechaLanzamiento, Descripcion
        FROM Versiones_Sistema
        WHERE Activo = 1
        ORDER BY FechaLanzamiento DESC";

            return await connection.QueryFirstOrDefaultAsync<VersionInfoDto>(sql)
                ?? new VersionInfoDto(); // ← Fallback
        }

        private bool EsVersionNueva(string versionNueva, string versionActual)
        {
            try
            {
                var nueva = new Version(versionNueva);
                var actual = new Version(versionActual);
                return nueva > actual;
            }
            catch
            {
                return versionNueva != versionActual;
            }
        }
    }
}