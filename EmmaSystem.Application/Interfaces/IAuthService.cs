using EmmaSystem.Application.DTOs.Auth;

namespace EmmaSystem.Application.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Login en EmmaSystemCentral. Valida credenciales y retorna lista de empresas disponibles.
    /// </summary>
    Task<LoginCentralResponseDto> LoginCentralAsync(LoginCentralRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Selecciona una empresa específica y genera token con contexto completo de tenant.
    /// </summary>
    Task<SeleccionEmpresaResponseDto> SeleccionarEmpresaAsync(int idUsuarioCentral, int idEmpresa, CancellationToken ct = default);

    /// <summary>
    /// Login directo a una empresa específica (usuario operativo).
    /// </summary>
    Task<SeleccionEmpresaResponseDto> LoginEmpresaAsync(LoginEmpresaRequestDto request, CancellationToken ct = default);
}