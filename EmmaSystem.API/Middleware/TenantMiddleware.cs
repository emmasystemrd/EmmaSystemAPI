using EmmaSystem.Application.Interfaces;

namespace EmmaSystem.API.Middleware;

/// <summary>
/// Middleware que establece el contexto del tenant (cliente y empresa) 
/// para cada request autenticado
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // Verificar si el usuario está autenticado
        if (context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                // Buscar los claims necesarios
                var clienteIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == "ClienteId");
                var empresaIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == "EmpresaId");
                var nombreEmpresaClaim = context.User.Claims.FirstOrDefault(c => c.Type == "NombreEmpresa");

                // Validar que existan todos los claims requeridos
                if (clienteIdClaim is null || empresaIdClaim is null || nombreEmpresaClaim is null)
                {
                    _logger.LogWarning(
                        "Request sin claims de tenant. ClienteId: {ClienteId}, EmpresaId: {EmpresaId}",
                        clienteIdClaim?.Value ?? "null",
                        empresaIdClaim?.Value ?? "null");
                }
                else
                {
                    // Establecer el contexto del tenant
                    var clienteId = int.Parse(clienteIdClaim.Value);
                    var empresaId = int.Parse(empresaIdClaim.Value);
                    var nombreEmpresa = nombreEmpresaClaim.Value;

                    tenantContext.Establecer(clienteId, empresaId, nombreEmpresa);

                    _logger.LogDebug(
                        "Tenant establecido: ClienteId={ClienteId}, EmpresaId={EmpresaId}, Empresa={NombreEmpresa}",
                        clienteId, empresaId, nombreEmpresa);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al establecer el contexto del tenant");
                // No lanzamos excepción aquí para no bloquear el request,
                // pero el repositorio fallará si intenta acceder a la BD sin tenant
            }
        }

        // Continuar con el siguiente middleware en el pipeline
        await _next(context);
    }
}