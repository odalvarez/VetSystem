using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            e.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);

            // bcrypt siempre produce 60 caracteres; 72 da margen ante cualquier variante
            e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(72);
            e.Property(u => u.Phone).HasMaxLength(20);

            // Columna estrecha + CHECK garantizan que nunca entre un rol inventado
            e.Property(u => u.Role).HasConversion<string>().IsRequired().HasMaxLength(20);
            e.HasCheckConstraint("CK_Users_Role",
                "[Role] IN ('Owner', 'Veterinarian', 'Admin')");

            e.Property(u => u.IsActive).HasDefaultValue(true);

            // El correo es la clave de búsqueda natural más frecuente
            e.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email");

            // El panel admin filtra siempre por IsActive y opcionalmente por Role
            e.HasIndex(u => new { u.IsActive, u.Role }).HasDatabaseName("IX_Users_IsActive_Role");

            e.Ignore(u => u.FullName);
        });
    }
}
