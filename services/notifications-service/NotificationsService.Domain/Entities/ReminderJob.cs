namespace NotificationsService.Domain.Entities;

public class ReminderJob
{
    public Guid     Id              { get; private set; }
    public Guid     AppointmentId   { get; private set; }
    public string   PatientName     { get; private set; } = default!;
    public string   OwnerName       { get; private set; } = default!;
    public string   OwnerPhone      { get; private set; } = default!;
    public string?  OwnerEmail      { get; private set; }
    public DateTime AppointmentAt   { get; private set; }
    public DateTime ScheduledSendAt { get; private set; }
    public string   Channels        { get; private set; } = default!;
    public bool     Sent            { get; private set; }
    public DateTime CreatedAt       { get; private set; }

    private ReminderJob() { }

    public static ReminderJob Create(
        Guid appointmentId, string patientName, string ownerName,
        string ownerPhone, string? ownerEmail, DateTime appointmentAt,
        IEnumerable<string> channels)
    {
        return new ReminderJob
        {
            Id              = Guid.NewGuid(),
            AppointmentId   = appointmentId,
            PatientName     = patientName,
            OwnerName       = ownerName,
            OwnerPhone      = ownerPhone,
            OwnerEmail      = ownerEmail,
            AppointmentAt   = appointmentAt,
            // 24 horas antes de la cita
            ScheduledSendAt = appointmentAt.AddHours(-24),
            Channels        = string.Join(",", channels),
            Sent            = false,
            CreatedAt       = DateTime.UtcNow
        };
    }

    public void MarkSent() => Sent = true;
}
