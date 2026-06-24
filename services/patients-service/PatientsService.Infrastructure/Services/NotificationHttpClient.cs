using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using PatientsService.Application.Interfaces;

namespace PatientsService.Infrastructure.Services;

public class NotificationHttpClient : INotificationClient
{
    private readonly HttpClient _http;
    private readonly ILogger<NotificationHttpClient> _logger;

    public NotificationHttpClient(HttpClient http, ILogger<NotificationHttpClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task SendPatientRegisteredAsync(
        Guid patientId, string patientName, string ownerName,
        string ownerPhone, string? ownerEmail,
        CancellationToken ct = default)
    {
        var message = $"Hola {ownerName}, tu mascota *{patientName}* ha sido registrada en VetSystem. " +
                      "Ya puedes consultar su ficha y agendar citas desde tu cuenta.";

        var html = $"""
            <div style="font-family:sans-serif;max-width:600px;margin:auto">
              <h2 style="color:#0B7285">🏥 VetSystem — Mascota registrada</h2>
              <p>Hola <strong>{ownerName}</strong>,</p>
              <p>Tu mascota <strong>{patientName}</strong> ha sido registrada exitosamente en VetSystem.</p>
              <p>Ya puedes consultar su ficha clínica y agendar citas desde tu cuenta.</p>
              <hr style="border-color:#E2E8F0"/>
              <p style="font-size:12px;color:#64748B">Este es un mensaje automático de VetSystem. No respondas a este correo.</p>
            </div>
            """;

        if (!string.IsNullOrWhiteSpace(ownerPhone))
            await TrySendAsync("api/notifications/whatsapp",
                new { to = ownerPhone, message },
                $"registro mascota WhatsApp {patientId}", ct);

        if (!string.IsNullOrWhiteSpace(ownerEmail))
            await TrySendAsync("api/notifications/email",
                new { to = ownerEmail, subject = $"Mascota registrada — {patientName}", body = html },
                $"registro mascota email {patientId}", ct);
    }

    private async Task TrySendAsync(string endpoint, object payload, string label, CancellationToken ct)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(endpoint, payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("notifications-service rechazó {Label}: {Status} — {Body}",
                    label, (int)response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar {Label}.", ex.Message);
        }
    }
}
