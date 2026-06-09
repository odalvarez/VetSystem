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
            e.Property(a => a.OwnerName).IsRequired().HasMaxLength(200);
            e.Property(a => a.OwnerPhone).IsRequired().HasMaxLength(20);
            e.Property(a => a.VeterinarianName).IsRequired().HasMaxLength(200);
            e.Property(a => a.Reason).IsRequired().HasMaxLength(500);
            e.Property(a => a.Notes).HasMaxLength(1000);

            // Columna estrecha + CHECK bloquean estados inválidos a nivel de BD
            e.Property(a => a.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
            e.HasCheckConstraint("CK_Appointments_Status",
                "[Status] IN ('Scheduled', 'Confirmed', 'Completed', 'Cancelled', 'NoShow')");

            // La misma regla de negocio que valida la entidad, replicada en la BD
            e.HasCheckConstraint("CK_Appointments_Duration",
                "[DurationMinutes] >= 10 AND [DurationMinutes] <= 480");

            // Índices de consulta frecuente
            e.HasIndex(a => new { a.VeterinarianId, a.ScheduledAt })
             .HasDatabaseName("IX_Appointments_VeterinarianId_ScheduledAt");
            e.HasIndex(a => a.OwnerId).HasDatabaseName("IX_Appointments_OwnerId");
            e.HasIndex(a => a.PatientId).HasDatabaseName("IX_Appointments_PatientId");
            e.HasIndex(a => a.ScheduledAt).HasDatabaseName("IX_Appointments_ScheduledAt");

            e.Ignore(a => a.EndsAt);
        });
    }
}
