using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthService.API;

public static class AuthRateLimiting
{
    public const string Login    = "login";
    public const string Register = "register";

    // Proxies en los que se confía para leer X-Forwarded-For: nginx del frontend y nginx-proxy viven
    // en redes Docker privadas. Una cabecera enviada desde Internet no puede suplantar la IP del cliente.
    private static readonly string[] TrustedProxyNetworks =
        ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16", "127.0.0.0/8", "::1/128"];

    public static void AddClientIpResolution(this IServiceCollection services) =>
        services.Configure<ForwardedHeadersOptions>(opt =>
        {
            opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            opt.KnownNetworks.Clear();
            opt.KnownProxies.Clear();
            foreach (var cidr in TrustedProxyNetworks)
                opt.KnownNetworks.Add(Microsoft.AspNetCore.HttpOverrides.IPNetwork.Parse(cidr));
            // Cadena esperada: cliente → nginx-proxy → nginx del frontend → auth-service
            opt.ForwardLimit = 3;
        });

    public static void AddAuthRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(opt =>
        {
            opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opt.AddPolicy(Login,    ctx => PerClient(ctx, "Login",    defaultLimit: 10));
            opt.AddPolicy(Register, ctx => PerClient(ctx, "Register", defaultLimit: 5));
        });

    // La configuración se lee por petición: así los límites se pueden ajustar por entorno
    // (más altos en E2E, desactivados en pruebas de integración) sin depender del orden de arranque.
    // Cada cliente tiene su propia ventana: quien agota el cupo no bloquea el login de los demás.
    private static RateLimitPartition<string> PerClient(HttpContext ctx, string policy, int defaultLimit)
    {
        var section = ctx.RequestServices.GetRequiredService<IConfiguration>().GetSection("RateLimiting");
        if (!section.GetValue("Enabled", true))
            return RateLimitPartition.GetNoLimiter("sin-limite");

        var cfg = section.GetSection(policy);
        return RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = cfg.GetValue("PermitLimit", defaultLimit),
                Window      = TimeSpan.FromMinutes(cfg.GetValue("WindowMinutes", 10)),
                QueueLimit  = 0
            });
    }
}
