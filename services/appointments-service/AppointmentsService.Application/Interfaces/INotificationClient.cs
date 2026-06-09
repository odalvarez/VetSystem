namespace AppointmentsService.Application.Interfaces;

/// <summary>
/// Abstracción del notifications-service desde el punto de vista de appointments.
/// Se inyecta en la capa Application para no acoplar la lógica de negocio al HTTP.
/// </summary>
public interface INotificationClient
{
    /// <summary>
    /// Programa el recordatorio de la cita 24h antes.
    /// Si falla, no debe interrumpir el flujo principal; el log es suficiente evidencia.
    /// </summary>
    Task ScheduleReminderAsync(
        Guid   appointmentId,
        string patientName,
        string ownerName,
        string ownerPhone,
        string ownerEmail,
        DateTime scheduledAt,
        CancellationToken ct = default);
}
