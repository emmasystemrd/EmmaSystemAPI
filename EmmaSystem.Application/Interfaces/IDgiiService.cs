using EmmaSystem.Application.DTOs.Dgii;

namespace EmmaSystem.Application.Interfaces;

public interface IDgiiService
{
    Task<ContribuyenteDto?> ConsultarRncAsync(string rncOCedula);
    Task<RncRegistradoDto?> ConsultarRncRegistradoAsync(string rncOCedula);
    Task<NcfValidationDto?> ConsultarNcfAsync(string ncf, string rncEmisor);
    Task<ENcfValidationDto?> ConsultarENcfAsync(string rncEmisor, string encf, string rncComprador, string codigoSeguridad);
}