namespace EmmaSystem.API.Middleware;

/// <summary>
/// Extensiones para registrar middlewares de forma limpia
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Agrega el middleware de Tenant al pipeline de la aplicación
    /// </summary>
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}