using EmmaSystem.Application.Exceptions;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Text.Json;

namespace EmmaSystem.API.Middleware;

public sealed class SqlExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SqlExceptionMiddleware> _logger;

    public SqlExceptionMiddleware(RequestDelegate next, ILogger<SqlExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SqlException capturado: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "DB_ERROR", message = ex.Message }));
        }
        catch (EmmaSystemException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.ErrorCode, message = ex.Message }));
        }
    }
}