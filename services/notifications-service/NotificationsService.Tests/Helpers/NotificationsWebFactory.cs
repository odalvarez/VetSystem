using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NotificationsService.Application.Interfaces;
using NotificationsService.Infrastructure.Data;

namespace NotificationsService.Tests.Helpers;

file sealed class NoOpWhatsAppSender : IWhatsAppSender
{
    public Task SendAsync(string to, string message, CancellationToken ct = default)
        => Task.CompletedTask;
}

file sealed class NoOpEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        => Task.CompletedTask;
}

public class NotificationsWebFactory : WebApplicationFactory<Program>
{
    public const string InternalKey = "test-internal-key";

    public static readonly Guid VetId   = Guid.NewGuid();
    public static readonly Guid OwnerId = Guid.NewGuid();
    public static readonly Guid AdminId = Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]                = JwtTestHelper.Secret,
                ["Jwt:Issuer"]                = JwtTestHelper.Issuer,
                ["Jwt:Audience"]              = JwtTestHelper.Audience,
                ["InternalKey"]               = InternalKey,
                ["ConnectionStrings:Default"] = "InMemory",
                ["Smtp:Host"]                 = "localhost",
                ["Smtp:Port"]                 = "25",
                ["EvolutionApi:BaseUrl"]      = "http://localhost",
                ["EvolutionApi:ApiKey"]       = "fake"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<NotificationsDbContext>));
            services.RemoveAll(typeof(DbContextOptions<NotificationsDbContext>));
            services.RemoveAll(typeof(NotificationsDbContext));

            services.AddDbContext<NotificationsDbContext>(opt =>
                opt.UseInMemoryDatabase("NotificationsTestDb"));

            services.RemoveAll(typeof(IWhatsAppSender));
            services.RemoveAll(typeof(IEmailSender));
            services.AddScoped<IWhatsAppSender, NoOpWhatsAppSender>();
            services.AddScoped<IEmailSender,    NoOpEmailSender>();
        });
    }
}
