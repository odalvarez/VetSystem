using System.ComponentModel.DataAnnotations;

namespace AppointmentsService.Application.DTOs;

public class CreateAppointmentRequest
{
    [Required]                 public Guid     PatientId       { get; set; }
    [Required]                 public Guid     VeterinarianId  { get; set; }
    [Required]                 public DateTime ScheduledAt     { get; set; }
    [Range(10, 480)]           public int      DurationMinutes { get; set; } = 30;
    [Required][MaxLength(500)] public string   Reason          { get; set; } = default!;
    [MaxLength(1000)]          public string?  Notes           { get; set; }

    // Datos del dueño y mascota que el caller debe proveer
    // (en un sistema con API Gateway esto vendría del token; aquí lo recibimos del caller)
    [Required] public string PatientName  { get; set; } = default!;
    [Required] public string OwnerName    { get; set; } = default!;
    [Required] public string OwnerPhone   { get; set; } = default!;
    [Required] public Guid   OwnerId      { get; set; }
    [Required] public string VeterinarianName { get; set; } = default!;
}

public class UpdateAppointmentRequest
{
    [Required]        public DateTime ScheduledAt     { get; set; }
    [Range(10, 480)]  public int      DurationMinutes { get; set; } = 30;
    [Required][MaxLength(500)] public string Reason   { get; set; } = default!;
    [MaxLength(1000)] public string?  Notes           { get; set; }
}

public class ChangeStatusRequest
{
    [Required] public string Status { get; set; } = default!;
}

public class AppointmentResponse
{
    public Guid     Id               { get; set; }
    public Guid     PatientId        { get; set; }
    public string   PatientName      { get; set; } = default!;
    public Guid     OwnerId          { get; set; }
    public string   OwnerName        { get; set; } = default!;
    public string   OwnerPhone       { get; set; } = default!;
    public Guid     VeterinarianId   { get; set; }
    public string   VeterinarianName { get; set; } = default!;
    public DateTime ScheduledAt      { get; set; }
    public int      DurationMinutes  { get; set; }
    public string   Reason           { get; set; } = default!;
    public string   Status           { get; set; } = default!;
    public string?  Notes            { get; set; }
    public DateTime CreatedAt        { get; set; }
    public DateTime UpdatedAt        { get; set; }
}

public class AvailabilityResponse
{
    public string                  Date            { get; set; } = default!;
    public Guid                    VeterinarianId  { get; set; }
    public IEnumerable<TimeSlot>   AvailableSlots  { get; set; } = default!;
}

public class TimeSlot
{
    public DateTime Start { get; set; }
    public DateTime End   { get; set; }
}

public class PagedResponse<T>
{
    public IEnumerable<T> Items      { get; set; } = default!;
    public int            TotalCount { get; set; }
    public int            Page       { get; set; }
    public int            PageSize   { get; set; }
}
