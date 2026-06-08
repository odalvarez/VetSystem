using NotificationsService.Domain.Entities;

namespace NotificationsService.Application.Interfaces;

public interface INotificationRepository
{
    Task<NotificationRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(NotificationRecord record, CancellationToken ct = default);
    Task UpdateAsync(NotificationRecord record, CancellationToken ct = default);
    Task AddReminderAsync(ReminderJob job, CancellationToken ct = default);
    Task<IEnumerable<ReminderJob>> GetPendingRemindersAsync(DateTime before, CancellationToken ct = default);
    Task UpdateReminderAsync(ReminderJob job, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
