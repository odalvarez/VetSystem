using NotificationsService.Domain.Enums;

namespace NotificationsService.Domain.Entities;

public class NotificationRecord
{
    public Guid               Id        { get; private set; }
    public NotificationType   Type      { get; private set; }
    public string             Recipient { get; private set; } = default!;
    public string?            Subject   { get; private set; }
    public string             Body      { get; private set; } = default!;
    public NotificationStatus Status    { get; private set; }
    public DateTime?          SentAt    { get; private set; }
    public string?            Error     { get; private set; }
    public DateTime           CreatedAt { get; private set; }

    private NotificationRecord() { }

    public static NotificationRecord Create(
        NotificationType type, string recipient, string body, string? subject = null)
    {
        return new NotificationRecord
        {
            Id        = Guid.NewGuid(),
            Type      = type,
            Recipient = recipient,
            Subject   = subject,
            Body      = body,
            Status    = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        Error  = null;
    }

    public void MarkFailed(string error)
    {
        Status = NotificationStatus.Failed;
        Error  = error;
    }
}
