using System.ComponentModel.DataAnnotations;

namespace NotificationsService.Application.DTOs;

public class SendWhatsAppRequest
{
    [Required] [MaxLength(20)]   public string To      { get; set; } = default!;
    [Required] [MaxLength(4096)] public string Message { get; set; } = default!;
}

public class SendEmailRequest
{
    [Required] [EmailAddress]    public string To      { get; set; } = default!;
    [Required] [MaxLength(200)]  public string Subject { get; set; } = default!;
    [Required]                   public string Body    { get; set; } = default!;
}

public class ScheduleReminderRequest
{
    [Required] public Guid     AppointmentId { get; set; }
    [Required] public string   PatientName   { get; set; } = default!;
    [Required] public string   OwnerName     { get; set; } = default!;
    [Required] public string   OwnerPhone    { get; set; } = default!;
    [EmailAddress] public string? OwnerEmail { get; set; }
    [Required] public DateTime ScheduledAt   { get; set; }
    [Required] public List<string> Channels  { get; set; } = default!;
}

public class NotificationAcceptedResponse
{
    public Guid     Id        { get; set; }
    public string   Status    { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class ReminderAcceptedResponse
{
    public Guid     ReminderId      { get; set; }
    public DateTime ScheduledSendAt { get; set; }
    public List<string> Channels    { get; set; } = default!;
}

public class NotificationStatusResponse
{
    public Guid      Id        { get; set; }
    public string    Type      { get; set; } = default!;
    public string    Recipient { get; set; } = default!;
    public string?   Subject   { get; set; }
    public string    Body      { get; set; } = default!;
    public string    Status    { get; set; } = default!;
    public DateTime? SentAt    { get; set; }
    public string?   Error     { get; set; }
    public DateTime  CreatedAt { get; set; }
}
