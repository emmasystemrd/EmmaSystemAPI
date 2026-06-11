namespace EmmaSystem.Application.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int idAcceso, int idModulo, int idOperacion, CancellationToken ct = default);
}