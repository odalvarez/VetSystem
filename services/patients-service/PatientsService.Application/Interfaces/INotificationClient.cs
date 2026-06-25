namespace PatientsService.Application.Interfaces;

public interface INotificationClient
{
    Task SendPatientRegisteredAsync(
        Guid    patientId,
        string  patientName,
        string  ownerName,
        string  ownerPhone,
        string? ownerEmail,
        CancellationToken ct = default);

    Task SendVaccinationReminderAsync(
        string  patientName,
        string  ownerName,
        string  ownerPhone,
        string? ownerEmail,
        string  vaccineName,
        string  nextDueDate,
        int     daysUntilDue,
        CancellationToken ct = default);
}
