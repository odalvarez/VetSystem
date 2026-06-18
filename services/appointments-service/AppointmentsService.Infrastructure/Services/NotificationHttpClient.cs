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

    public async Task SendConfirmationAsync(
        Guid appointmentId, string patientName, string ownerName,
        string ownerPhone, string? ownerEmail, string veterinarianName,
        DateTime scheduledAt, int durationMinutes, string reason,
        CancellationToken ct = default)
    {
        var message = BuildConfirmationMessage(ownerName, patientName, veterinarianName, scheduledAt, durationMinutes, reason);
        var subject = $"Cita agendada — {patientName}";
        var html    = BuildConfirmationHtml(ownerName, patientName, veterinarianName, scheduledAt, durationMinutes, reason);

        await TrySendAsync("api/notifications/whatsapp",
            new { to = ownerPhone, message },
            $"confirmación WhatsApp para cita {appointmentId}", ct);

        if (!string.IsNullOrWhiteSpace(ownerEmail))
            await TrySendAsync("api/notifications/email",
                new { to = ownerEmail, subject, body = html },
                $"confirmación email para cita {appointmentId}", ct);
    }

    public async Task SendReminderNowAsync(
        Guid appointmentId, string patientName, string ownerName,
        string ownerPhone, string? ownerEmail, DateTime scheduledAt,
        CancellationToken ct = default)
    {
        var message = BuildMessage(ownerName, patientName, scheduledAt);
        var subject = $"Recordatorio de cita — {patientName}";
        var html    = BuildHtml(ownerName, patientName, scheduledAt);

        await TrySendAsync(
            "api/notifications/whatsapp",
            new { to = ownerPhone, message },
            $"WhatsApp para cita {appointmentId}",
            ct);

        if (!string.IsNullOrWhiteSpace(ownerEmail))
            await TrySendAsync(
                "api/notifications/email",
                new { to = ownerEmail, subject, body = html },
                $"email para cita {appointmentId}",
                ct);
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
            _logger.LogError(ex, "No se pudo enviar {Label}.", label);
        }
    }

    private static string BuildConfirmationMessage(
        string ownerName, string patientName, string veterinarianName,
        DateTime scheduledAt, int durationMinutes, string reason)
        => $"Hola {ownerName}, tu cita veterinaria para {patientName} fue agendada exitosamente.\n" +
           $"📅 Fecha: {scheduledAt:dd/MM/yyyy}\n" +
           $"🕐 Hora: {scheduledAt:HH:mm} ({durationMinutes} min)\n" +
           $"👨‍⚕️ Veterinario: {veterinarianName}\n" +
           $"📋 Motivo: {reason}\n" +
           $"Recuerda traer la cartilla de vacunación.";

    private static string BuildConfirmationHtml(
        string ownerName, string patientName, string veterinarianName,
        DateTime scheduledAt, int durationMinutes, string reason)
        => $"""
            <div style="font-family:sans-serif;max-width:600px;margin:auto">
              <h2 style="color:#0B7285">🏥 VetSystem — Cita agendada</h2>
              <p>Hola <strong>{ownerName}</strong>, tu cita fue registrada exitosamente.</p>
              <table style="border-collapse:collapse;width:100%">
                <tr><td style="padding:6px;color:#64748B">Mascota</td><td style="padding:6px"><strong>{patientName}</strong></td></tr>
                <tr style="background:#F8FAFC"><td style="padding:6px;color:#64748B">Fecha</td><td style="padding:6px"><strong>{scheduledAt:dd/MM/yyyy}</strong></td></tr>
                <tr><td style="padding:6px;color:#64748B">Hora</td><td style="padding:6px"><strong>{scheduledAt:HH:mm}</strong> ({durationMinutes} min)</td></tr>
                <tr style="background:#F8FAFC"><td style="padding:6px;color:#64748B">Veterinario</td><td style="padding:6px"><strong>{veterinarianName}</strong></td></tr>
                <tr><td style="padding:6px;color:#64748B">Motivo</td><td style="padding:6px">{reason}</td></tr>
              </table>
              <p style="margin-top:16px">Por favor llega 10 minutos antes y trae la cartilla de vacunación.</p>
              <hr style="border-color:#E2E8F0"/>
              <p style="font-size:12px;color:#64748B">Este es un mensaje automático de VetSystem. No respondas a este correo.</p>
            </div>
            """;

    private static string BuildMessage(string ownerName, string patientName, DateTime scheduledAt)
        => $"Hola {ownerName}, te recordamos que mañana tienes cita veterinaria para {patientName} " +
           $"a las {scheduledAt:HH:mm}. ¡No olvides llevar la cartilla de vacunación!";

    private static string BuildHtml(string ownerName, string patientName, DateTime scheduledAt)
        => $"""
            <div style="font-family:sans-serif;max-width:600px;margin:auto">
              <h2 style="color:#0B7285">🏥 VetSystem — Recordatorio de cita</h2>
              <p>Hola <strong>{ownerName}</strong>,</p>
              <p>Te recordamos que mañana tienes cita veterinaria para <strong>{patientName}</strong>
                 a las <strong>{scheduledAt:HH:mm}</strong>.</p>
              <p>Por favor llega 10 minutos antes y trae la cartilla de vacunación.</p>
              <hr style="border-color:#E2E8F0"/>
              <p style="font-size:12px;color:#64748B">
                Este es un mensaje automático de VetSystem. No respondas a este correo.
              </p>
            </div>
            """;
}
