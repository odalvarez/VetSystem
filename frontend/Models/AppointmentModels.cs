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
    public string?  OwnerEmail       { get; set; }
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

public class ClinicSettingsResponse
{
    public string       StartTime { get; set; } = "08:00";
    public string       EndTime   { get; set; } = "20:00";
    public List<string> WorkDays  { get; set; } = new();
}

public class UpdateClinicSettingsRequest
{
    public string       StartTime { get; set; } = "08:00";
    public string       EndTime   { get; set; } = "20:00";
    public List<string> WorkDays  { get; set; } = new();
}

public class VeterinarianScheduleResponse
{
    public Guid   VeterinarianId { get; set; }
    public string DayOfWeek      { get; set; } = "";
    public string StartTime      { get; set; } = "";
    public string EndTime        { get; set; } = "";
}

public class UpsertVeterinarianScheduleRequest
{
    public string DayOfWeek { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndTime   { get; set; } = "";
}

public class VeterinarianLeaveResponse
{
    public Guid   Id             { get; set; }
    public Guid   VeterinarianId { get; set; }
    public string DateFrom       { get; set; } = "";
    public string DateTo         { get; set; } = "";
    public string Reason         { get; set; } = "";
}

public class CreateVeterinarianLeaveRequest
{
    public string DateFrom { get; set; } = "";
    public string DateTo   { get; set; } = "";
    public string Reason   { get; set; } = "";
}
