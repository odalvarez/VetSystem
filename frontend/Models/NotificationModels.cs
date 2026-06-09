namespace VetSystem.Frontend.Models;

public class NotificationStatusResponse
{
    public Guid      Id           { get; set; }
    public string    Type         { get; set; } = "";
    public string    Recipient    { get; set; } = "";
    public string?   Subject      { get; set; }
    public string    Body         { get; set; } = "";
    public string    Status       { get; set; } = "";
    public DateTime? SentAt       { get; set; }
    public string?   Error        { get; set; }
    public DateTime  CreatedAt    { get; set; }
}

public class SendWhatsAppRequest
{
    public string To      { get; set; } = "";
    public string Message { get; set; } = "";
}

public class SendEmailRequest
{
    public string To      { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body    { get; set; } = "";
}

public class ScheduleReminderRequest
{
    public Guid         AppointmentId { get; set; }
    public string       PatientName   { get; set; } = "";
    public string       OwnerName     { get; set; } = "";
    public string       OwnerPhone    { get; set; } = "";
    public string       OwnerEmail    { get; set; } = "";
    public DateTime     ScheduledAt   { get; set; }
    public List<string> Channels      { get; set; } = new();
}

public class ApiError
{
    public string? Title  { get; set; }
    public string? Detail { get; set; }
    public int     Status { get; set; }
}
