using NotificationsService.Application.Interfaces;

namespace NotificationsService.API;

/// <summary>
/// Proceso en background que revisa cada 5 minutos si hay recordatorios cuya
/// ScheduledSendAt ya llegó y los despacha. Así el "24h antes" funciona de verdad
/// en lugar de enviar inmediatamente al crear el recordatorio.
/// </summary>
public class ReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderWorker> _logger;

    // El intervalo de polling; 5 minutos es suficiente para este caso de uso
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public ReminderWorker(IServiceScopeFactory scopeFactory, ILogger<ReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderWorker iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // No dejamos caer el worker por un error en un ciclo
                _logger.LogError(ex, "Error en el ciclo de despacho de recordatorios.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task DispatchPendingRemindersAsync(CancellationToken ct)
    {
        // Scope nuevo por ciclo porque el DbContext y los senders son Scoped
        using var scope = _scopeFactory.CreateScope();
        var repo     = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppSender>();
        var email    = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var pending = (await repo.GetPendingRemindersAsync(DateTime.UtcNow, ct)).ToList();

        if (pending.Count == 0) return;

        _logger.LogInformation("Despachando {Count} recordatorio(s).", pending.Count);

        foreach (var job in pending)
        {
            var channels = job.Channels
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().ToLowerInvariant())
                .ToHashSet();

            var message = BuildReminderMessage(job.OwnerName, job.PatientName, job.AppointmentAt);
            var subject = $"Recordatorio de cita — {job.PatientName}";

            // Intentamos WhatsApp y email de forma independiente para que un fallo
            // en un canal no bloquee el otro
            if (channels.Contains("whatsapp") && !string.IsNullOrEmpty(job.OwnerPhone))
            {
                try
                {
                    await whatsApp.SendAsync(job.OwnerPhone, message, ct);
                    _logger.LogInformation("WhatsApp enviado para cita {Id}.", job.AppointmentId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falló WhatsApp para cita {Id}.", job.AppointmentId);
                }
            }

            if (channels.Contains("email") && !string.IsNullOrEmpty(job.OwnerEmail))
            {
                try
                {
                    var htmlBody = BuildReminderHtml(job.OwnerName, job.PatientName, job.AppointmentAt);
                    await email.SendAsync(job.OwnerEmail, subject, htmlBody, ct);
                    _logger.LogInformation("Email enviado para cita {Id}.", job.AppointmentId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falló email para cita {Id}.", job.AppointmentId);
                }
            }

            // Marcamos como enviado aunque algún canal haya fallado; el log queda como evidencia
            job.MarkSent();
            await repo.UpdateReminderAsync(job, ct);
            // Guardamos por job para que un fallo de BD no genere reenvíos duplicados en el siguiente ciclo
            await repo.SaveChangesAsync(ct);
        }
    }

    private static string BuildReminderMessage(string ownerName, string patientName, DateTime appointmentAt)
        => $"Hola {ownerName}, te recordamos que mañana tienes cita veterinaria para {patientName} " +
           $"a las {appointmentAt:HH:mm}. ¡No olvides llevar la cartilla de vacunación!";

    private static string BuildReminderHtml(string ownerName, string patientName, DateTime appointmentAt)
        => $"""
            <div style="font-family:sans-serif;max-width:600px;margin:auto">
              <h2 style="color:#0B7285">🏥 VetSystem — Recordatorio de cita</h2>
              <p>Hola <strong>{ownerName}</strong>,</p>
              <p>Te recordamos que mañana tienes cita veterinaria para <strong>{patientName}</strong>
                 a las <strong>{appointmentAt:HH:mm}</strong>.</p>
              <p>Por favor llega 10 minutos antes y trae la cartilla de vacunación.</p>
              <hr style="border-color:#E2E8F0"/>
              <p style="font-size:12px;color:#64748B">
                Este es un mensaje automático de VetSystem. No respondas a este correo.
              </p>
            </div>
            """;
}
