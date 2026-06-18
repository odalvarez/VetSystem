using AppointmentsService.Application.Interfaces;
using AppointmentsService.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppointmentsService.Tests.Helpers;

file sealed class NoOpNotificationClient : INotificationClient
{
    public Task SendConfirmationAsync(
        Guid appointmentId, string patientName, string ownerName,
        string ownerPhone, string? ownerEmail, string veterinarianName,
        DateTime scheduledAt, int durationMinutes, string reason,
        CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SendReminderNowAsync(
        Guid appointmentId, string patientName, string ownerName,
        string ownerPhone, string? ownerEmail, DateTime scheduledAt,
        CancellationToken ct = default)
        => Task.CompletedTask;
}

public class AppointmentsWebFactory : WebApplicationFactory<Program>
{
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
                ["Jwt:Secret"]                      = JwtTestHelper.Secret,
                ["Jwt:Issuer"]                      = JwtTestHelper.Issuer,
                ["Jwt:Audience"]                    = JwtTestHelper.Audience,
                ["ConnectionStrings:Default"]       = "InMemory",
                ["NotificationsService:BaseUrl"]    = "http://localhost",
                ["NotificationsService:InternalKey"] = "test-key"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppointmentsDbContext>));
            services.RemoveAll(typeof(DbContextOptions<AppointmentsDbContext>));
            services.RemoveAll(typeof(AppointmentsDbContext));

            services.AddDbContext<AppointmentsDbContext>(opt =>
                opt.UseInMemoryDatabase("AppointmentsTestDb"));

            services.RemoveAll(typeof(INotificationClient));
            services.AddScoped<INotificationClient, NoOpNotificationClient>();
        });
    }
}
