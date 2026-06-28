using System.Threading.Tasks;

namespace EmmaSystem.Application.Interfaces
{
    public interface IVersionService
    {
        Task<VersionResponseDto> VerificarActualizacionAsync(string versionActual);
        Task<VersionInfoDto> ObtenerUltimaVersionAsync();
    }

    public class VersionResponseDto
    {
        public bool HayActualizacion { get; set; }
        public string VersionNueva { get; set; }
        public string URL_Instalador { get; set; }
        public string URL_ScriptSQL { get; set; }
        public string Descripcion { get; set; }
        public bool EsObligatorio { get; set; }
    }

    public class VersionInfoDto
    {
        public string Version { get; set; }
        public string FechaLanzamiento { get; set; }
        public string Descripcion { get; set; }
    }
}