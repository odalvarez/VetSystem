using System.Net.Http.Json;
using AppointmentsService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AppointmentsService.Infrastructure.Services;

public class NotificationHttpClient : INotificationClient
{
    private readonly HttpClient _http;
    private readonly ILogger<NotificationHttpClient> _logger;

    public NotificationHttpClient(HttpClient http, ILogger<NotificationHttpClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task ScheduleReminderAsync(
        Guid appointmentId, string patientName, string ownerName,
        string ownerPhone, string ownerEmail, DateTime scheduledAt,
        CancellationToken ct = default)
    {
        var payload = new
        {
            appointmentId,
            patientName,
            ownerName,
            ownerPhone,
            ownerEmail,
            scheduledAt,
            // Enviamos por ambos canales si el propietario tiene teléfono y correo
            channels = BuildChannels(ownerPhone, ownerEmail)
        };

        try
        {
            var response = await _http.PostAsJsonAsync("api/notifications/reminder", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "notifications-service rechazó el recordatorio para cita {Id}: {Status} — {Body}",
                    appointmentId, (int)response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            // No propagamos la excepción: si el notifications-service está caído no queremos
            // que eso revierta la confirmación de la cita en el appointments-service
            _logger.LogError(ex,
                "No se pudo programar el recordatorio para la cita {Id}.", appointmentId);
        }
    }

    private static List<string> BuildChannels(string phone, string email)
    {
        var channels = new List<string>();
        if (!string.IsNullOrWhiteSpace(phone)) channels.Add("whatsapp");
        if (!string.IsNullOrWhiteSpace(email)) channels.Add("email");
        // Si por alguna razón no hay ningún canal, usamos whatsapp por defecto
        if (channels.Count == 0) channels.Add("whatsapp");
        return channels;
    }
}
