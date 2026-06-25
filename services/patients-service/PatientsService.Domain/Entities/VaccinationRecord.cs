using PatientsService.Domain.Exceptions;

namespace PatientsService.Domain.Entities;

public class VaccinationRecord
{
    public Guid      Id                  { get; private set; }
    public Guid      PatientId           { get; private set; }
    public string    PatientName         { get; private set; } = default!;
    public Guid      OwnerId             { get; private set; }
    public string    OwnerName           { get; private set; } = default!;
    public string    OwnerPhone          { get; private set; } = default!;
    public string?   OwnerEmail          { get; private set; }
    public Guid      VaccineDefinitionId { get; private set; }
    public string    VaccineName         { get; private set; } = default!;
    public int       DoseNumber          { get; private set; }
    public DateOnly  AdministeredAt      { get; private set; }
    public Guid      AdministeredById    { get; private set; }
    public string    AdministeredByName  { get; private set; } = default!;
    public string?   BatchNumber         { get; private set; }
    public DateOnly? NextDueDate         { get; private set; }
    public string?   Notes               { get; private set; }
    public DateTime  CreatedAt           { get; private set; }

    // Seguimiento de recordatorios enviados
    public DateTime? Reminder7SentAt     { get; private set; }
    public DateTime? Reminder2SentAt     { get; private set; }

    private VaccinationRecord() { }

    public static VaccinationRecord Create(
        Guid patientId, string patientName,
        Guid ownerId, string ownerName, string ownerPhone, string? ownerEmail,
        Guid vaccineDefinitionId, string vaccineName,
        int doseNumber, DateOnly administeredAt,
        Guid administeredById, string administeredByName,
        DateOnly? nextDueDate,
        string? batchNumber = null, string? notes = null)
    {
        if (doseNumber < 1)
            throw new DomainException("El número de dosis debe ser mayor a cero.");

        return new VaccinationRecord
        {
            Id                  = Guid.NewGuid(),
            PatientId           = patientId,
            PatientName         = patientName,
            OwnerId             = ownerId,
            OwnerName           = ownerName,
            OwnerPhone          = ownerPhone,
            OwnerEmail          = ownerEmail,
            VaccineDefinitionId = vaccineDefinitionId,
            VaccineName         = vaccineName,
            DoseNumber          = doseNumber,
            AdministeredAt      = administeredAt,
            AdministeredById    = administeredById,
            AdministeredByName  = administeredByName,
            NextDueDate         = nextDueDate,
            BatchNumber         = batchNumber?.Trim(),
            Notes               = notes?.Trim(),
            CreatedAt           = DateTime.UtcNow
        };
    }

    public void OverrideNextDueDate(DateOnly? date) => NextDueDate = date;

    public void MarkReminder7Sent() => Reminder7SentAt = DateTime.UtcNow;
    public void MarkReminder2Sent() => Reminder2SentAt = DateTime.UtcNow;
}
