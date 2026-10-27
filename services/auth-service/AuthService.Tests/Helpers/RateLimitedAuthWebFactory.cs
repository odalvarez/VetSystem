using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Tests.Helpers;

// Igual que AuthWebFactory pero con el limitador activo y límites bajos, y con las peticiones
// llegando desde una IP de la red Docker, como ocurre detrás de nginx
public class RateLimitedAuthWebFactory : AuthWebFactory
{
    public const int LoginLimit    = 2;
    public const int RegisterLimit = 1;

    protected override string DatabaseName => "AuthRateLimitTestDb";

    // IP desde la que llega la conexión a auth-service; por defecto, un proxy de la red Docker
    protected virtual string RemoteIp => "172.18.0.5";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Enabled"]               = "true",
                ["RateLimiting:Login:PermitLimit"]     = LoginLimit.ToString(),
                ["RateLimiting:Register:PermitLimit"]  = RegisterLimit.ToString()
            });
        });

        builder.ConfigureServices(services =>
            services.AddTransient<IStartupFilter>(_ => new RemoteIpFilter(RemoteIp)));
    }

    private sealed class RemoteIpFilter(string remoteIp) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((ctx, nextMw) =>
            {
                ctx.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
                return nextMw();
            });
            next(app);
        };
    }
}
