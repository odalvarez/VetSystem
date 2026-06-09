using Microsoft.EntityFrameworkCore;
using NotificationsService.Application.Interfaces;
using NotificationsService.Domain.Entities;

namespace NotificationsService.Infrastructure.Data.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationsDbContext _db;

    public NotificationRepository(NotificationsDbContext db) => _db = db;

    public Task<NotificationRecord?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IEnumerable<NotificationRecord>> ListAllAsync(
        int page, int pageSize, IReadOnlyList<string>? recipientFilter, CancellationToken ct)
    {
        var query = _db.Notifications.AsQueryable();

        // Si hay filtro de destinatario (propietario), solo sus notificaciones
        if (recipientFilter != null && recipientFilter.Count > 0)
            query = query.Where(n => recipientFilter.Contains(n.Recipient));

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task AddAsync(NotificationRecord record, CancellationToken ct) =>
        await _db.Notifications.AddAsync(record, ct);

    public Task UpdateAsync(NotificationRecord record, CancellationToken ct)
    {
        _db.Notifications.Update(record);
        return Task.CompletedTask;
    }

    public async Task AddReminderAsync(ReminderJob job, CancellationToken ct) =>
        await _db.Reminders.AddAsync(job, ct);

    public Task<IEnumerable<ReminderJob>> GetPendingRemindersAsync(DateTime before, CancellationToken ct) =>
        Task.FromResult<IEnumerable<ReminderJob>>(
            _db.Reminders.Where(r => !r.Sent && r.ScheduledSendAt <= before).ToList());

    public Task UpdateReminderAsync(ReminderJob job, CancellationToken ct)
    {
        _db.Reminders.Update(job);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) =>
        _db.SaveChangesAsync(ct);
}
