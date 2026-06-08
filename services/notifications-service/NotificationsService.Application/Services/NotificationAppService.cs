using NotificationsService.Application.DTOs;
using NotificationsService.Application.Exceptions;
using NotificationsService.Application.Interfaces;
using NotificationsService.Domain.Entities;
using NotificationsService.Domain.Enums;

namespace NotificationsService.Application.Services;

public class NotificationAppService
{
    private readonly INotificationRepository _repo;
    private readonly IWhatsAppSender         _whatsApp;
    private readonly IEmailSender            _email;

    public NotificationAppService(
        INotificationRepository repo,
        IWhatsAppSender whatsApp,
        IEmailSender email)
    {
        _repo     = repo;
        _whatsApp = whatsApp;
        _email    = email;
    }

    public async Task<NotificationAcceptedResponse> SendWhatsAppAsync(
        SendWhatsAppRequest req, CancellationToken ct)
    {
        var record = NotificationRecord.Create(NotificationType.WhatsApp, req.To, req.Message);
        await _repo.AddAsync(record, ct);
        await _repo.SaveChangesAsync(ct);

        // Fire and forget: si falla se marca como Failed en base de datos
        _ = SendWhatsAppInBackground(record, req.To, req.Message);

        return new NotificationAcceptedResponse
        {
            Id = record.Id, Status = "pending", CreatedAt = record.CreatedAt
        };
    }

    public async Task<NotificationAcceptedResponse> SendEmailAsync(
        SendEmailRequest req, CancellationToken ct)
    {
        var record = NotificationRecord.Create(NotificationType.Email, req.To, req.Body, req.Subject);
        await _repo.AddAsync(record, ct);
        await _repo.SaveChangesAsync(ct);

        _ = SendEmailInBackground(record, req.To, req.Subject, req.Body);

        return new NotificationAcceptedResponse
        {
            Id = record.Id, Status = "pending", CreatedAt = record.CreatedAt
        };
    }

    public async Task<ReminderAcceptedResponse> ScheduleReminderAsync(
        ScheduleReminderRequest req, CancellationToken ct)
    {
        if (req.ScheduledAt <= DateTime.UtcNow.AddHours(1))
            throw new ValidationException("La cita debe ser al menos 1 hora en el futuro para programar un recordatorio.");

        var job = ReminderJob.Create(
            req.AppointmentId, req.PatientName, req.OwnerName,
            req.OwnerPhone, req.OwnerEmail, req.ScheduledAt, req.Channels);

        await _repo.AddReminderAsync(job, ct);
        await _repo.SaveChangesAsync(ct);

        return new ReminderAcceptedResponse
        {
            ReminderId      = job.Id,
            ScheduledSendAt = job.ScheduledSendAt,
            Channels        = job.Channels.Split(',').ToList()
        };
    }

    public async Task<NotificationStatusResponse> GetStatusAsync(Guid id, CancellationToken ct)
    {
        var record = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Notificación no encontrada.");

        return new NotificationStatusResponse
        {
            Id        = record.Id,
            Type      = record.Type.ToString().ToLowerInvariant(),
            Recipient = record.Recipient,
            Subject   = record.Subject,
            Body      = record.Body,
            Status    = record.Status.ToString().ToLowerInvariant(),
            SentAt    = record.SentAt,
            Error     = record.Error,
            CreatedAt = record.CreatedAt
        };
    }

    private async Task SendWhatsAppInBackground(NotificationRecord record, string to, string message)
    {
        try
        {
            await _whatsApp.SendAsync(to, message);
            record.MarkSent();
        }
        catch (Exception ex)
        {
            record.MarkFailed(ex.Message);
        }
        finally
        {
            await _repo.UpdateAsync(record, CancellationToken.None);
            await _repo.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task SendEmailInBackground(NotificationRecord record, string to, string subject, string body)
    {
        try
        {
            await _email.SendAsync(to, subject, body);
            record.MarkSent();
        }
        catch (Exception ex)
        {
            record.MarkFailed(ex.Message);
        }
        finally
        {
            await _repo.UpdateAsync(record, CancellationToken.None);
            await _repo.SaveChangesAsync(CancellationToken.None);
        }
    }
}
