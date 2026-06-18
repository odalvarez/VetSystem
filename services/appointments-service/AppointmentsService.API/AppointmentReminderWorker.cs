using AppointmentsService.Application.Interfaces;

namespace AppointmentsService.API;

/// <summary>
/// Se ejecuta a las 13:00 UTC (08:00 Colombia). Busca citas del día siguiente con
/// ReminderSent = false y despacha sus recordatorios. Al arrancar verifica si ya
/// pasaron las 13:00 UTC y el job aún no corrió hoy, para cubrir reinicios tardíos.
/// </summary>
public class AppointmentReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentReminderWorker> _logger;

    // 13:00 UTC = 08:00 hora Colombia (UTC-5)
    private static readonly TimeSpan TargetUtcHour = TimeSpan.FromHours(13);

    public AppointmentReminderWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AppointmentReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AppointmentReminderWorker iniciado.");

        // Si arrancamos después de las 13:00 UTC, enviamos de inmediato los pendientes de hoy
        // en lugar de esperar hasta mañana
        var now          = DateTime.UtcNow;
        var todayTarget  = now.Date.Add(TargetUtcHour);
        if (now >= todayTarget)
        {
            await TryDispatchAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun();
            _logger.LogInformation("Próxima ejecución de recordatorios en {Minutes} minutos.", (int)delay.TotalMinutes);

            await Task.Delay(delay, stoppingToken);

            await TryDispatchAsync(stoppingToken);
        }
    }

    private async Task TryDispatchAsync(CancellationToken ct)
    {
        try
        {
            await DispatchAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al despachar recordatorios diarios.");
        }
    }

    private async Task DispatchAsync(CancellationToken ct)
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        _logger.LogInformation("Enviando recordatorios para citas del {Date}.", tomorrow);

        using var scope = _scopeFactory.CreateScope();
        var repo          = scope.ServiceProvider.GetRequiredService<IAppointmentRepository>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationClient>();

        var appointments = (await repo.GetScheduledForDateAsync(tomorrow, ct)).ToList();

        if (appointments.Count == 0)
        {
            _logger.LogInformation("Sin citas pendientes de recordatorio para mañana.");
            return;
        }

        _logger.LogInformation("Enviando {Count} recordatorio(s).", appointments.Count);

        foreach (var appt in appointments)
        {
            await notifications.SendReminderNowAsync(
                appointmentId: appt.Id,
                patientName:   appt.PatientName,
                ownerName:     appt.OwnerName,
                ownerPhone:    appt.OwnerPhone,
                ownerEmail:    null,
                scheduledAt:   appt.ScheduledAt,
                ct:            ct);

            appt.MarkReminderSent();
            await repo.UpdateAsync(appt, ct);
            // Guardamos por cita para que un fallo a mitad no cause reenvíos del lote completo
            await repo.SaveChangesAsync(ct);
        }
    }

    private static TimeSpan GetDelayUntilNextRun()
    {
        var now     = DateTime.UtcNow;
        var next    = now.Date.Add(TargetUtcHour);
        if (next <= now)
            next = next.AddDays(1);
        return next - now;
    }
}
