using AppointmentsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppointmentsService.Infrastructure.Data;

public class AppointmentsDbContext : DbContext
{
    public AppointmentsDbContext(DbContextOptions<AppointmentsDbContext> options) : base(options) { }

    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Appointment>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.PatientName).IsRequired().HasMaxLength(100);
            e.Property(a => a.OwnerName).HasMaxLength(200);
            e.Property(a => a.OwnerPhone).HasMaxLength(20);
            e.Property(a => a.VeterinarianName).HasMaxLength(200);
            e.Property(a => a.Reason).IsRequired().HasMaxLength(500);
            e.Property(a => a.Notes).HasMaxLength(1000);
            e.Property(a => a.Status).HasConversion<string>();

            // Consultas frecuentes: por veterinario y rango de fecha
            e.HasIndex(a => new { a.VeterinarianId, a.ScheduledAt });
            e.HasIndex(a => a.OwnerId);

            e.Ignore(a => a.EndsAt);
        });
    }
}
