namespace VetSystem.Frontend.Models;

public class CreateAppointmentRequest
{
    public Guid     PatientId        { get; set; }
    public Guid     VeterinarianId   { get; set; }
    public DateTime ScheduledAt      { get; set; } = DateTime.UtcNow.AddDays(1);
    public int      DurationMinutes  { get; set; } = 30;
    public string   Reason           { get; set; } = "";
    public string?  Notes            { get; set; }
    // El appointments-service no llama a patients-service; el frontend los incluye
    public string   PatientName      { get; set; } = "";
    public string   OwnerName        { get; set; } = "";
    public string   OwnerPhone       { get; set; } = "";
    public Guid     OwnerId          { get; set; }
    public string   VeterinarianName { get; set; } = "";
}

public class UpdateAppointmentRequest
{
    public DateTime ScheduledAt     { get; set; }
    public int      DurationMinutes { get; set; } = 30;
    public string   Reason          { get; set; } = "";
    public string?  Notes           { get; set; }
}

public class ChangeStatusRequest
{
    public string Status { get; set; } = "";
}

public class AppointmentResponse
{
    public Guid     Id               { get; set; }
    public Guid     PatientId        { get; set; }
    public string   PatientName      { get; set; } = "";
    public Guid     OwnerId          { get; set; }
    public string   OwnerName        { get; set; } = "";
    public string   OwnerPhone       { get; set; } = "";
    public Guid     VeterinarianId   { get; set; }
    public string   VeterinarianName { get; set; } = "";
    public DateTime ScheduledAt      { get; set; }
    public int      DurationMinutes  { get; set; }
    public string   Reason           { get; set; } = "";
    public string   Status           { get; set; } = "";
    public string?  Notes            { get; set; }
    public DateTime CreatedAt        { get; set; }
    public DateTime UpdatedAt        { get; set; }
}

public class AvailabilityResponse
{
    public string         Date           { get; set; } = "";
    public Guid           VeterinarianId { get; set; }
    public List<TimeSlot> AvailableSlots { get; set; } = new();
}

public class TimeSlot
{
    public DateTime Start { get; set; }
    public DateTime End   { get; set; }
}
