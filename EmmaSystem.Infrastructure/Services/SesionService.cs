using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Repositories;

namespace EmmaSystem.Infrastructure.Services;

public class SesionService : ISesionService
{
    private readonly SesionRepository _sesionRepo;

    public SesionService(SesionRepository sesionRepo)
    {
        _sesionRepo = sesionRepo;
    }

    public async Task<(bool PuedeIniciar, string Mensaje)> ValidarSesionSimultaneaAsync(int idCliente, CancellationToken ct)
    {
        var sesionesActivas = await _sesionRepo.ContarSesionesActivasAsync(idCliente, ct);
        var maxConcurrentes = await _sesionRepo.GetMaxConcurrentesAsync(idCliente, ct);

        if (sesionesActivas >= maxConcurrentes)
        {
            return (false,
                $"Su plan permite un máximo de {maxConcurrentes} sesión(es) simultánea(s). " +
                $"Actualmente hay {sesionesActivas} sesión(es) activa(s). " +
                $"Cierre sesión en otro dispositivo o actualice su plan para continuar.");
        }

        return (true, "OK");
    }

    public async Task RegistrarSesionAsync(int idUsuarioCentral, int idCliente, int? idEmpresa,
        string token, string ipAddress, string userAgent, string deviceId, string nombreEquipo,
        CancellationToken ct)
        => await _sesionRepo.RegistrarSesionAsync(idUsuarioCentral, idCliente, idEmpresa,
            token, ipAddress, userAgent, deviceId, nombreEquipo, ct);

    public async Task HeartbeatAsync(string token, CancellationToken ct)
    {
        var actualizado = await _sesionRepo.ActualizarUltimoActividadAsync(token, ct);
        if (!actualizado)
            throw new InvalidOperationException("Sesión no encontrada o ya cerrada.");
    }

    public async Task CerrarSesionAsync(string token, CancellationToken ct)
        => await _sesionRepo.CerrarSesionAsync(token, ct);

    public async Task<int> LimpiarSesionesInactivasAsync(int minutosInactividad = 30, CancellationToken ct = default)
        => await _sesionRepo.LimpiarSesionesInactivasAsync(minutosInactividad, ct);

    public async Task<(bool PuedeCrear, string Mensaje, int actuales, int maximo)> ValidarCreacionEmpresaAsync(int idCliente, CancellationToken ct)
    {
        var empresasActivas = await _sesionRepo.ContarEmpresasActivasAsync(idCliente, ct);
        var maxEmpresas = await _sesionRepo.GetMaxEmpresasAsync(idCliente, ct);

        if (empresasActivas >= maxEmpresas)
        {
            return (false,
                $"Su plan actual permite un máximo de {maxEmpresas} empresa(s). " +
                $"Actualmente tiene {empresasActivas} empresa(s) registrada(s). " +
                $"Para agregar más empresas, actualice su plan.",
                empresasActivas, maxEmpresas);
        }

        return (true, "OK", empresasActivas, maxEmpresas);
    }

    public async Task<bool> ExisteSesionDispositivoAsync(int idCliente, string deviceId, CancellationToken ct)
        => await _sesionRepo.ExisteSesionDispositivoAsync(idCliente, deviceId, ct);

    public async Task CerrarSesionDispositivoAsync(int idCliente, string deviceId, CancellationToken ct)
        => await _sesionRepo.CerrarSesionDispositivoAsync(idCliente, deviceId, ct);

    /// <summary>
    /// NUEVA IMPLEMENTACIÓN: Conecta la llamada de control con el repositorio de aislamiento exclusivo.
    /// </summary>
    public async Task<(bool PuedeIniciar, string Mensaje)> ValidarYRegistrarSesionAtomicaAsync(
        int idUsuarioCentral, int idCliente, int? idEmpresa, string token,
        string ipAddress, string userAgent, string deviceId, string nombreEquipo,
        int maxConcurrentes, CancellationToken ct)
    {
        return await _sesionRepo.ValidarYRegistrarSesionAtomicaAsync(
            idUsuarioCentral,
            idCliente,
            idEmpresa,
            token,
            ipAddress,
            userAgent,
            deviceId,
            nombreEquipo,
            maxConcurrentes,
            ct);
    }
}