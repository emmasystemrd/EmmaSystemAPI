namespace EmmaSystem.Application.Interfaces;

public interface ISesionService
{
    Task<(bool PuedeIniciar, string Mensaje)> ValidarSesionSimultaneaAsync(int idCliente, CancellationToken ct);

    Task RegistrarSesionAsync(int idUsuarioCentral, int idCliente, int? idEmpresa,
        string token, string ipAddress, string userAgent, string deviceId, string nombreEquipo,
        CancellationToken ct);

    Task HeartbeatAsync(string token, CancellationToken ct);

    Task CerrarSesionAsync(string token, CancellationToken ct);

    Task<int> LimpiarSesionesInactivasAsync(int minutosInactividad = 30, CancellationToken ct = default);

    Task<(bool PuedeCrear, string Mensaje, int actuales, int maximo)> ValidarCreacionEmpresaAsync(int idCliente, CancellationToken ct);

    Task<bool> ExisteSesionDispositivoAsync(int idCliente, string deviceId, CancellationToken ct);

    Task CerrarSesionDispositivoAsync(int idCliente, string deviceId, CancellationToken ct);

    /// <summary>
    /// NUEVO: Registra y valida la sesión bajo un bloqueo exclusivo de base de datos.
    /// </summary>
    Task<(bool PuedeIniciar, string Mensaje)> ValidarYRegistrarSesionAtomicaAsync(
        int idUsuarioCentral, int idCliente, int? idEmpresa, string token,
        string ipAddress, string userAgent, string deviceId, string nombreEquipo,
        int maxConcurrentes, CancellationToken ct);
}