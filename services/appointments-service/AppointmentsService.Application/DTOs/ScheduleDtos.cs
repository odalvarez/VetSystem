using System.ComponentModel.DataAnnotations;

namespace AppointmentsService.Application.DTOs;

// ── ClinicSettings ────────────────────────────────────────────────────────────

public class ClinicSettingsResponse
{
    public string       StartTime { get; set; } = default!;
    public string       EndTime   { get; set; } = default!;
    public List<string> WorkDays  { get; set; } = default!;
}

public class UpdateClinicSettingsRequest
{
    [Required] public string       StartTime { get; set; } = default!;
    [Required] public string       EndTime   { get; set; } = default!;
    [Required] public List<string> WorkDays  { get; set; } = default!;
}

// ── VeterinarianSchedule ──────────────────────────────────────────────────────

public class VeterinarianScheduleResponse
{
    public Guid   VeterinarianId { get; set; }
    public string DayOfWeek      { get; set; } = default!;
    public string StartTime      { get; set; } = default!;
    public string EndTime        { get; set; } = default!;
}

public class UpsertVeterinarianScheduleRequest
{
    [Required] public string DayOfWeek { get; set; } = default!;
    [Required] public string StartTime { get; set; } = default!;
    [Required] public string EndTime   { get; set; } = default!;
}

// ── VeterinarianLeave ─────────────────────────────────────────────────────────

public class VeterinarianLeaveResponse
{
    public Guid    Id             { get; set; }
    public Guid    VeterinarianId { get; set; }
    public string  DateFrom       { get; set; } = default!;
    public string  DateTo         { get; set; } = default!;
    public string? StartTime      { get; set; }
    public string? EndTime        { get; set; }
    public string  Reason         { get; set; } = default!;
}

public class CreateVeterinarianLeaveRequest
{
    [Required]                 public string  DateFrom  { get; set; } = default!;
    [Required]                 public string  DateTo    { get; set; } = default!;
    [Required][MaxLength(500)] public string  Reason    { get; set; } = default!;
    public string? StartTime { get; set; }
    public string? EndTime   { get; set; }
}
