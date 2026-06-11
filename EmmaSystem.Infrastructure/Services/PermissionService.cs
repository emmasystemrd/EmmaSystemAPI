using EmmaSystem.Application.Interfaces;
using EmmaSystem.Infrastructure.Repositories;

namespace EmmaSystem.Infrastructure.Services;

public sealed class PermissionService : IPermissionService
{
    private readonly PermissionRepository _repo;
    public PermissionService(PermissionRepository repo) => _repo = repo;

    public Task<bool> HasPermissionAsync(int idAcceso, int idModulo, int idOperacion, CancellationToken ct)
        => _repo.HasPermissionAsync(idAcceso, idModulo, idOperacion, ct);
}