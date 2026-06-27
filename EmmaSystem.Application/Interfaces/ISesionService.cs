namespace EmmaSystem.Application.Interfaces;

public interface ISesionService
{
    Task<(bool PuedeIniciar, string Mensaje)> ValidarSesionSimultaneaAsync(int idCliente, CancellationToken ct);
    Task RegistrarSesionAsync(int idUsuarioCentral, int idCliente, int? idEmpresa,
        string token, string ipAddress, string userAgent, CancellationToken ct);
    Task HeartbeatAsync(string token, CancellationToken ct);
    Task CerrarSesionAsync(string token, CancellationToken ct);
    Task<int> LimpiarSesionesInactivasAsync(int minutosInactividad = 30, CancellationToken ct = default);
    Task<(bool PuedeCrear, string Mensaje, int actuales, int maximo)> ValidarCreacionEmpresaAsync(int idCliente, CancellationToken ct);
}