using System.Text.Json;
using AppointmentsService.Application.Exceptions;

namespace AppointmentsService.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next   = next;
        _logger = logger;
        _env    = env;
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
            AppointmentsService.Domain.Exceptions.DomainException => (400, "Regla de negocio"),
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
            detail   = status == 500 && _env.IsProduction() ? "Error interno del servidor." : ex.Message,
            instance = ctx.Request.Path.Value
        }));
    }
}
