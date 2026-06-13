using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PatientsService.Domain.Entities;
using PatientsService.Infrastructure.Data;

namespace PatientsService.Tests.Helpers;

public class PatientsWebFactory : WebApplicationFactory<Program>
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
                ["Jwt:Secret"]                = JwtTestHelper.Secret,
                ["Jwt:Issuer"]                = JwtTestHelper.Issuer,
                ["Jwt:Audience"]              = JwtTestHelper.Audience,
                ["ConnectionStrings:Default"] = "InMemory"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<PatientsDbContext>));
            services.RemoveAll(typeof(DbContextOptions<PatientsDbContext>));
            services.RemoveAll(typeof(PatientsDbContext));

            services.AddDbContext<PatientsDbContext>(opt =>
                opt.UseInMemoryDatabase("PatientsTestDb"));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();

        // Los tests envían Species="Canine"/"Feline" → slug resultante es "canine"/"feline"
        db.Species.AddRange(
            Species.Create("Canine", "canine"),
            Species.Create("Feline", "feline")
        );
        db.SaveChanges();

        return host;
    }
}
