using EmmaSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EmmaSystem.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class PermissionAttribute : Attribute
{
    public int IdModulo { get; }
    public int IdOperacion { get; }
    public PermissionAttribute(int idModulo, int idOperacion) { IdModulo = idModulo; IdOperacion = idOperacion; }
}

public sealed class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IPermissionService _permissionService;
    public PermissionAuthorizationFilter(IPermissionService permissionService) => _permissionService = permissionService;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var attr = context.ActionDescriptor.EndpointMetadata.OfType<PermissionAttribute>().FirstOrDefault();
        if (attr is null) return;

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true) { context.Result = new UnauthorizedResult(); return; }

        var idAccesoStr = user.FindFirst("Idacceso")?.Value;
        if (!int.TryParse(idAccesoStr, out var idAcceso)) { context.Result = new UnauthorizedResult(); return; }

        var allowed = await _permissionService.HasPermissionAsync(idAcceso, attr.IdModulo, attr.IdOperacion, context.HttpContext.RequestAborted);
        if (!allowed)
        {
            context.Result = new ObjectResult(new { error = "FORBIDDEN", message = $"Sin permisos para Módulo={attr.IdModulo}, Operación={attr.IdOperacion}." })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}