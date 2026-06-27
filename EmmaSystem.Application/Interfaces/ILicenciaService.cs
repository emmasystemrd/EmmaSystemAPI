using EmmaSystem.Application.DTOs.Auth;

namespace EmmaSystem.Application.Interfaces;

public interface ILicenciaService
{
    Task<LicenciaValidationResultDto> ValidarAsync(int idCliente, byte[] secretKey, CancellationToken ct);
}