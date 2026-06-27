using EmmaSystem.Application.DTOs.Admin;

namespace EmmaSystem.Application.Interfaces;

public interface IAdminService
{
    /// <summary>
    /// Valida todos los requisitos antes de registrar un cliente.
    /// Se puede llamar desde el frontend antes de enviar el formulario completo.
    /// </summary>
    Task<ValidarRegistroResponseDto> ValidarRegistroAsync(
        RegistrarClienteRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Registra un nuevo cliente con usuario central, licencia y empresa en una transacción atómica.
    /// Copia EmmaSystem_Template, genera salt aleatorio, hashea password, cifra cadena de conexión.
    /// </summary>
    Task<RegistrarClienteResponseDto> RegistrarClienteAsync(
        RegistrarClienteRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Registra un cliente con una base de datos existente (migración).
    /// Valida que la BD exista y pertenezca a EmmaSystem.
    /// </summary>
    Task<RegistrarClienteResponseDto> RegistrarBDExistenteAsync(
        RegistrarBDExistenteRequestDto request, CancellationToken ct = default);
}