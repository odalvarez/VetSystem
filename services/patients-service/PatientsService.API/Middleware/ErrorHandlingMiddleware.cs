using System.Text.Json;
using PatientsService.Application.Exceptions;

namespace PatientsService.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex) { await HandleAsync(ctx, ex); }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (status, title) = ex switch
        {
            ConflictException     => (409, "Conflicto"),
            NotFoundException     => (404, "No encontrado"),
            ForbiddenException    => (403, "Sin permiso"),
            UnauthorizedException => (401, "No autorizado"),
            ValidationException   => (400, "Validación fallida"),
            _                     => (500, "Error interno del servidor")
        };

        if (status == 500)
            _logger.LogError(ex, "Error no controlado");

        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/problem+json";

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            type     = $"https://httpstatuses.com/{status}",
            title,
            status,
            detail   = ex.Message,
            instance = ctx.Request.Path.Value
        }));
    }
}
