using EmmaSystem.Application.Interfaces;

namespace EmmaSystem.API.Middleware;

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
        if (context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                // ✅ Usar los nombres EXACTOS de los claims generados en AuthService
                var clienteIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == "ClienteId");
                var empresaIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == "Idempresa"); // Minúscula 'e' como en BuildJwtTenant
                var nombreEmpresaClaim = context.User.Claims.FirstOrDefault(c => c.Type == "NombreEmpresa");
                var rncCedulaClaim = context.User.Claims.FirstOrDefault(c => c.Type == "RncCedula");
                var ambienteClaim = context.User.Claims.FirstOrDefault(c => c.Type == "Ambiente");

                if (clienteIdClaim is not null && empresaIdClaim is not null)
                {
                    var clienteId = int.Parse(clienteIdClaim.Value);
                    var empresaId = int.Parse(empresaIdClaim.Value);
                    var nombreEmpresa = nombreEmpresaClaim?.Value ?? "";
                    var rncCedula = rncCedulaClaim?.Value ?? "";
                    var ambiente = byte.TryParse(ambienteClaim?.Value, out var amb) ? amb : (byte)1;

                    tenantContext.Establecer(clienteId, empresaId, nombreEmpresa, rncCedula, ambiente);

                    _logger.LogDebug(
                        "✅ Tenant establecido: Cliente={ClienteId}, Empresa={EmpresaId}, BD={NombreEmpresa}, RNC={RncCedula}, Amb={Ambiente}",
                        clienteId, empresaId, nombreEmpresa, rncCedula, ambiente);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al establecer el contexto del tenant");
            }
        }

        await _next(context);
    }
}