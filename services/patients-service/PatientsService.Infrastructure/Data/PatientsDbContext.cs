using Microsoft.EntityFrameworkCore;
using PatientsService.Domain.Entities;

namespace PatientsService.Infrastructure.Data;

public class PatientsDbContext : DbContext
{
    public PatientsDbContext(DbContextOptions<PatientsDbContext> options) : base(options) { }

    public DbSet<Patient>          Patients          => Set<Patient>();
    public DbSet<ClinicalRecord>   ClinicalRecords   => Set<ClinicalRecord>();
    public DbSet<Species>          Species           => Set<Species>();
    public DbSet<ConsultationLog>  ConsultationLogs  => Set<ConsultationLog>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Patient>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(100);
            e.Property(p => p.Breed).IsRequired().HasMaxLength(100);

            e.Property(p => p.SpeciesId).IsRequired();

            e.Property(p => p.Sex).HasConversion<string>().IsRequired().HasMaxLength(10);
            e.ToTable(t => t.HasCheckConstraint("CK_Patients_Sex", "[Sex] IN ('Male', 'Female')"));

            e.Property(p => p.Weight).HasColumnType("decimal(6,2)");
            e.ToTable(t => t.HasCheckConstraint("CK_Patients_Weight", "[Weight] > 0"));

            e.Property(p => p.Color).HasMaxLength(100);
            e.Property(p => p.MicrochipNumber).HasMaxLength(50);
            e.Property(p => p.OwnerName).IsRequired().HasMaxLength(200);
            e.Property(p => p.OwnerPhone).HasMaxLength(30).HasDefaultValue("");

            e.HasIndex(p => p.OwnerId).HasDatabaseName("IX_Patients_OwnerId");
            e.HasIndex(p => p.Name).HasDatabaseName("IX_Patients_Name");
            e.HasIndex(p => p.SpeciesId).HasDatabaseName("IX_Patients_SpeciesId");
        });

        model.Entity<ClinicalRecord>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Reason).IsRequired().HasMaxLength(500);
            e.Property(r => r.Diagnosis).IsRequired().HasMaxLength(500);
            e.Property(r => r.Treatment).IsRequired().HasMaxLength(500);
            e.Property(r => r.Notes).HasMaxLength(1000);
            e.Property(r => r.WeightKg).HasColumnType("decimal(6,2)");
            e.Property(r => r.TemperatureCelsius).HasColumnType("decimal(4,1)");
            e.Property(r => r.VeterinarianName).IsRequired().HasMaxLength(200);

            e.HasOne(r => r.Patient)
             .WithMany(p => p.ClinicalRecords)
             .HasForeignKey(r => r.PatientId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(r => r.Date).HasDatabaseName("IX_ClinicalRecords_Date");
            e.HasIndex(r => r.VeterinarianId).HasDatabaseName("IX_ClinicalRecords_VeterinarianId");
        });

        model.Entity<ConsultationLog>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Status).IsRequired().HasMaxLength(10);
            e.Property(l => l.ReasonForVisit).IsRequired().HasMaxLength(500);
            e.Property(l => l.Anamnesis).HasMaxLength(2000);
            e.Property(l => l.HeartRate).HasMaxLength(100);
            e.Property(l => l.RespiratoryRate).HasMaxLength(100);
            e.Property(l => l.BodyCondition).HasMaxLength(200);
            e.Property(l => l.MucousMembranes).HasMaxLength(200);
            e.Property(l => l.Hydration).HasMaxLength(200);
            e.Property(l => l.WeightKg).HasColumnType("decimal(6,2)");
            e.Property(l => l.TemperatureCelsius).HasColumnType("decimal(4,1)");
            e.Property(l => l.RequestedTests).HasMaxLength(2000);
            e.Property(l => l.TestResults).HasMaxLength(2000);
            e.Property(l => l.Diagnosis).HasMaxLength(1000);
            e.Property(l => l.Prognosis).HasMaxLength(1000);
            e.Property(l => l.TherapeuticPlan).HasMaxLength(2000);
            e.Property(l => l.DiagnosticPlan).HasMaxLength(2000);
            e.Property(l => l.Recommendations).HasMaxLength(1000);
            e.Property(l => l.VeterinarianName).IsRequired().HasMaxLength(200);

            e.HasOne(l => l.Patient)
             .WithMany(p => p.ConsultationLogs)
             .HasForeignKey(l => l.PatientId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(l => l.PatientId).HasDatabaseName("IX_ConsultationLogs_PatientId");
            e.HasIndex(l => l.OpenedAt).HasDatabaseName("IX_ConsultationLogs_OpenedAt");
        });

        model.Entity<Species>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).IsRequired().HasMaxLength(100);
            e.Property(s => s.Slug).IsRequired().HasMaxLength(50);
            e.Property(s => s.IsActive).IsRequired().HasDefaultValue(true);
            e.Property(s => s.CreatedAt).IsRequired();

            // Slug único: no puede haber dos especies con el mismo identificador
            e.HasIndex(s => s.Slug).IsUnique().HasDatabaseName("IX_Species_Slug");
        });
    }
}
