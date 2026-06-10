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

public class ApiError
{
    public string? Title  { get; set; }
    public string? Detail { get; set; }
    public int     Status { get; set; }
}
