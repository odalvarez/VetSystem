using AuthService.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AuthService.Tests.Helpers;

public class AuthWebFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail    = "admin@test.com";
    public const string AdminPassword = "Test123#";

    public Guid AdminId { get; private set; }
    public Guid VetId   { get; private set; }
    public Guid OwnerId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]         = JwtTestHelper.Secret,
                ["Jwt:Issuer"]         = JwtTestHelper.Issuer,
                ["Jwt:Audience"]       = JwtTestHelper.Audience,
                ["AdminSeed:Email"]    = AdminEmail,
                ["AdminSeed:Password"] = AdminPassword,
                ["ConnectionStrings:Default"] = "InMemory"
            });
        });

        builder.ConfigureServices(services =>
        {
            // EF Core 9 registra IDbContextOptionsConfiguration<T> por cada llamada a AddDbContext.
            // Si no la removemos, queda la configuración de SQL Server y se acumulan dos providers.
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AuthDbContext>));
            services.RemoveAll(typeof(DbContextOptions<AuthDbContext>));
            services.RemoveAll(typeof(AuthDbContext));

            services.AddDbContext<AuthDbContext>(opt =>
                opt.UseInMemoryDatabase("AuthTestDb"));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<AuthService.Application.Interfaces.IPasswordHasher>();

        var admin = db.Users.First(u => u.Email == AdminEmail);
        AdminId = admin.Id;

        var hash  = hasher.Hash(AdminPassword);

        var vet = AuthService.Domain.Entities.User.Create(
            "Vet", "Test", "vet@test.com", hash, "3001111111",
            AuthService.Domain.Enums.UserRole.Veterinarian);

        var owner = AuthService.Domain.Entities.User.Create(
            "Owner", "Test", "owner@test.com", hash, "3002222222",
            AuthService.Domain.Enums.UserRole.Owner);

        db.Users.AddRange(vet, owner);
        db.SaveChanges();

        VetId   = vet.Id;
        OwnerId = owner.Id;

        return host;
    }
}
