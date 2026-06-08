using Microsoft.EntityFrameworkCore;
using NotificationsService.Domain.Entities;

namespace NotificationsService.Infrastructure.Data;

public class NotificationsDbContext : DbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options) { }

    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();
    public DbSet<ReminderJob>        Reminders     => Set<ReminderJob>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<NotificationRecord>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Recipient).IsRequired().HasMaxLength(256);
            e.Property(n => n.Subject).HasMaxLength(200);
            e.Property(n => n.Body).IsRequired();
            e.Property(n => n.Type).HasConversion<string>();
            e.Property(n => n.Status).HasConversion<string>();
            e.HasIndex(n => n.Status);
        });

        model.Entity<ReminderJob>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.PatientName).HasMaxLength(100);
            e.Property(r => r.OwnerName).HasMaxLength(200);
            e.Property(r => r.OwnerPhone).HasMaxLength(20);
            e.Property(r => r.OwnerEmail).HasMaxLength(256);
            e.Property(r => r.Channels).HasMaxLength(50);

            // Índice para el proceso que despacha recordatorios pendientes
            e.HasIndex(r => new { r.Sent, r.ScheduledSendAt });
        });
    }
}
