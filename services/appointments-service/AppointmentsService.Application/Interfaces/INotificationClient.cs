namespace AppointmentsService.Application.Interfaces;

public interface INotificationClient
{
    Task SendConfirmationAsync(
        Guid      appointmentId,
        string    patientName,
        string    ownerName,
        string    ownerPhone,
        string?   ownerEmail,
        string    veterinarianName,
        DateTime  scheduledAt,
        int       durationMinutes,
        string    reason,
        CancellationToken ct = default);

    Task SendReminderNowAsync(
        Guid      appointmentId,
        string    patientName,
        string    ownerName,
        string    ownerPhone,
        string?   ownerEmail,
        DateTime  scheduledAt,
        CancellationToken ct = default);
}
