using Microsoft.EntityFrameworkCore;
using PatientsService.Domain.Entities;

namespace PatientsService.Infrastructure.Data;

public class PatientsDbContext : DbContext
{
    public PatientsDbContext(DbContextOptions<PatientsDbContext> options) : base(options) { }

    public DbSet<Patient>        Patients        => Set<Patient>();
    public DbSet<ClinicalRecord> ClinicalRecords => Set<ClinicalRecord>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Patient>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(100);
            e.Property(p => p.Breed).HasMaxLength(100);
            e.Property(p => p.Species).HasConversion<string>();
            e.Property(p => p.Sex).HasConversion<string>();
            e.Property(p => p.Weight).HasColumnType("decimal(6,2)");
            e.Property(p => p.Color).HasMaxLength(100);
            e.Property(p => p.MicrochipNumber).HasMaxLength(50);
            e.Property(p => p.OwnerName).HasMaxLength(200);
            e.Property(p => p.OwnerPhone).HasMaxLength(30).HasDefaultValue("");
            e.HasIndex(p => p.OwnerId);
        });

        model.Entity<ClinicalRecord>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Reason).IsRequired().HasMaxLength(500);
            e.Property(r => r.Diagnosis).HasMaxLength(500);
            e.Property(r => r.Treatment).HasMaxLength(500);
            e.Property(r => r.Notes).HasMaxLength(1000);
            e.Property(r => r.WeightKg).HasColumnType("decimal(6,2)");
            e.Property(r => r.TemperatureCelsius).HasColumnType("decimal(4,1)");
            e.Property(r => r.VeterinarianName).HasMaxLength(200);

            e.HasOne(r => r.Patient)
             .WithMany(p => p.ClinicalRecords)
             .HasForeignKey(r => r.PatientId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
