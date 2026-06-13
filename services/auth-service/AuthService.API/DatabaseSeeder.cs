using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.API;

// Solo se ejecuta si la variable de entorno SeedReset=true está presente.
// Sirve para resetear usuarios en staging/dev sin necesitar acceso directo a la BD.
public static class DatabaseSeeder
{
    public static async Task ResetAndSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        db.Users.RemoveRange(await db.Users.ToListAsync());
        await db.SaveChangesAsync();

        var hash = hasher.Hash("Test123#");

        await db.Users.AddRangeAsync(
            User.Create("Admin",       "VetSystem", "admin@gmail.com", hash, "", UserRole.Admin),
            User.Create("Veterinario", "Demo",      "vet@gmail.com",   hash, "", UserRole.Veterinarian),
            User.Create("Propietario", "Demo",      "own@gmail.com",   hash, "", UserRole.Owner)
        );

        await db.SaveChangesAsync();
    }
}
