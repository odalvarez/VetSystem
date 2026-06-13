namespace NotificationsService.API.Middleware;

// Bloquea cualquier solicitud que no incluya la clave interna.
// Esto evita que el frontend u otros clientes externos llamen directamente a este servicio.
public class InternalKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string          _expectedKey;

    public InternalKeyMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next        = next;
        _expectedKey = config["InternalKey"]
            ?? throw new InvalidOperationException("InternalKey no configurada.");
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        // El healthcheck de Docker y Swagger no pueden enviar la clave interna
        if (ctx.Request.Path.StartsWithSegments("/health") ||
            ctx.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(ctx);
            return;
        }

        if (!ctx.Request.Headers.TryGetValue("X-Internal-Key", out var key) || key != _expectedKey)
        {
            ctx.Response.StatusCode = 403;
            await ctx.Response.WriteAsync("Acceso denegado: clave interna requerida.");
            return;
        }
        await _next(ctx);
    }
}
