using PatientsService.Domain.Exceptions;

namespace PatientsService.Domain.Entities;

public class ClinicalRecord
{
    public Guid      Id                 { get; private set; }
    public Guid      PatientId          { get; private set; }
    public DateTime  Date               { get; private set; }
    public string    Reason             { get; private set; } = default!;
    public string    Diagnosis          { get; private set; } = default!;
    public string    Treatment          { get; private set; } = default!;
    public string?   Notes              { get; private set; }
    public Guid      VeterinarianId     { get; private set; }
    public string    VeterinarianName   { get; private set; } = default!;
    public DateOnly? NextVisitDate      { get; private set; }
    public DateTime  CreatedAt          { get; private set; }

    public Patient Patient { get; private set; } = default!;

    private ClinicalRecord() { }

    public static ClinicalRecord Create(
        Guid patientId, DateTime date,
        string reason, string diagnosis, string treatment,
        string? notes, Guid veterinarianId, string veterinarianName,
        DateOnly? nextVisitDate)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("El motivo de la consulta es obligatorio.");

        return new ClinicalRecord
        {
            Id               = Guid.NewGuid(),
            PatientId        = patientId,
            Date             = date,
            Reason           = reason.Trim(),
            Diagnosis        = diagnosis.Trim(),
            Treatment        = treatment.Trim(),
            Notes            = notes?.Trim(),
            VeterinarianId   = veterinarianId,
            VeterinarianName = veterinarianName,
            NextVisitDate    = nextVisitDate,
            CreatedAt        = DateTime.UtcNow
        };
    }
}
