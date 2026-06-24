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
}
