using AppointmentsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppointmentsService.Infrastructure.Data;

public class AppointmentsDbContext : DbContext
{
    public AppointmentsDbContext(DbContextOptions<AppointmentsDbContext> options) : base(options) { }

    public DbSet<Appointment>           Appointments           => Set<Appointment>();
    public DbSet<ClinicSettings>        ClinicSettings         => Set<ClinicSettings>();
    public DbSet<VeterinarianSchedule>  VeterinarianSchedules  => Set<VeterinarianSchedule>();
    public DbSet<VeterinarianLeave>     VeterinarianLeaves     => Set<VeterinarianLeave>();

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
            e.ToTable(t => t.HasCheckConstraint("CK_Appointments_Status",
                "[Status] IN ('Scheduled', 'Confirmed', 'Completed', 'Cancelled', 'NoShow')"));

            // La misma regla de negocio que valida la entidad, replicada en la BD
            e.ToTable(t => t.HasCheckConstraint("CK_Appointments_Duration",
                "[DurationMinutes] >= 10 AND [DurationMinutes] <= 480"));

            // Índices de consulta frecuente
            e.HasIndex(a => new { a.VeterinarianId, a.ScheduledAt })
             .HasDatabaseName("IX_Appointments_VeterinarianId_ScheduledAt");
            e.HasIndex(a => a.OwnerId).HasDatabaseName("IX_Appointments_OwnerId");
            e.HasIndex(a => a.PatientId).HasDatabaseName("IX_Appointments_PatientId");
            e.HasIndex(a => a.ScheduledAt).HasDatabaseName("IX_Appointments_ScheduledAt");

            e.Ignore(a => a.EndsAt);
        });

        model.Entity<ClinicSettings>(e =>
        {
            e.HasKey(s => s.Id);
            // Singleton con Id fijo = 1; sin IDENTITY para poder insertar el valor explícito
            e.Property(s => s.Id).ValueGeneratedNever();
            e.Property(s => s.WorkDays).IsRequired().HasMaxLength(100);
            // TimeOnly se convierte a string HH:mm para compatibilidad con SQL Server
            e.Property(s => s.StartTime).HasConversion(
                t => t.ToString("HH:mm"),
                s => TimeOnly.Parse(s));
            e.Property(s => s.EndTime).HasConversion(
                t => t.ToString("HH:mm"),
                s => TimeOnly.Parse(s));
        });

        model.Entity<VeterinarianSchedule>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.DayOfWeek).HasConversion<string>().HasMaxLength(10);
            e.Property(s => s.StartTime).HasConversion(
                t => t.ToString("HH:mm"),
                s => TimeOnly.Parse(s));
            e.Property(s => s.EndTime).HasConversion(
                t => t.ToString("HH:mm"),
                s => TimeOnly.Parse(s));
            // Un vet solo puede tener un horario por día de la semana
            e.HasIndex(s => new { s.VeterinarianId, s.DayOfWeek }).IsUnique()
             .HasDatabaseName("IX_VeterinarianSchedule_VetId_Day");
        });

        model.Entity<VeterinarianLeave>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Reason).IsRequired().HasMaxLength(500);
            e.Property(l => l.DateFrom).HasConversion(
                d => d.ToDateTime(TimeOnly.MinValue),
                dt => DateOnly.FromDateTime(dt));
            e.Property(l => l.DateTo).HasConversion(
                d => d.ToDateTime(TimeOnly.MinValue),
                dt => DateOnly.FromDateTime(dt));
            e.Property(l => l.StartTime).HasConversion(
                t => t.HasValue ? t.Value.ToString("HH:mm") : null,
                s => s != null ? TimeOnly.Parse(s) : (TimeOnly?)null);
            e.Property(l => l.EndTime).HasConversion(
                t => t.HasValue ? t.Value.ToString("HH:mm") : null,
                s => s != null ? TimeOnly.Parse(s) : (TimeOnly?)null);
            e.HasIndex(l => new { l.VeterinarianId, l.DateFrom })
             .HasDatabaseName("IX_VeterinarianLeave_VetId_DateFrom");
        });
    }
}
