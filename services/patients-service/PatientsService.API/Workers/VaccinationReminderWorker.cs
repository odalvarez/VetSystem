using PatientsService.Application.Interfaces;

namespace PatientsService.API.Workers;

public class VaccinationReminderWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<VaccinationReminderWorker> logger) : BackgroundService
{
    // Hora local en que corre el worker (Colombia UTC-5)
    private const int RunHourUtc = 13; // 08:00 COT = 13:00 UTC

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelay();
            logger.LogInformation("VaccinationReminderWorker: próxima ejecución en {Minutes} minutos.", (int)delay.TotalMinutes);
            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            await RunAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope      = scopeFactory.CreateScope();
        var repo             = scope.ServiceProvider.GetRequiredService<IVaccinationRepository>();
        var notifications    = scope.ServiceProvider.GetRequiredService<INotificationClient>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var date7 = today.AddDays(7);
        var date2 = today.AddDays(2);

        logger.LogInformation("VaccinationReminderWorker: evaluando recordatorios para +7d ({D7}) y +2d ({D2}).", date7, date2);

        var pending7 = await repo.GetPendingReminder7Async(date7, ct);
        var pending2 = await repo.GetPendingReminder2Async(date2, ct);

        await ProcessBatchAsync(pending7, 7, repo, notifications, ct);
        await ProcessBatchAsync(pending2, 2, repo, notifications, ct);

        logger.LogInformation("VaccinationReminderWorker: fin. Enviados 7d={C7}, 2d={C2}.", pending7.Count, pending2.Count);
    }

    private async Task ProcessBatchAsync(
        IReadOnlyList<Domain.Entities.VaccinationRecord> records,
        int daysUntilDue,
        IVaccinationRepository repo,
        INotificationClient notifications,
        CancellationToken ct)
    {
        const int batchSize = 50;
        for (int i = 0; i < records.Count; i += batchSize)
        {
            var batch = records.Skip(i).Take(batchSize);
            foreach (var record in batch)
            {
                try
                {
                    await notifications.SendVaccinationReminderAsync(
                        patientName:  record.PatientName,
                        ownerName:    record.OwnerName,
                        ownerPhone:   record.OwnerPhone,
                        ownerEmail:   record.OwnerEmail,
                        vaccineName:  record.VaccineName,
                        nextDueDate:  record.NextDueDate!.Value.ToString("dd/MM/yyyy"),
                        daysUntilDue: daysUntilDue,
                        ct:           ct);

                    if (daysUntilDue == 7)
                        record.MarkReminder7Sent();
                    else
                        record.MarkReminder2Sent();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "VaccinationReminderWorker: fallo enviando recordatorio {Id}.", record.Id);
                }
            }

            await repo.SaveChangesAsync(ct);

            // Pausa entre lotes para no saturar Evolution API
            if (i + batchSize < records.Count)
                await Task.Delay(500, ct);
        }
    }

    private static TimeSpan ComputeDelay()
    {
        var now  = DateTime.UtcNow;
        var next = DateTime.UtcNow.Date.AddHours(RunHourUtc);
        if (next <= now)
            next = next.AddDays(1);
        return next - now;
    }
}
