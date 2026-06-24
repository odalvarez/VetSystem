using AppointmentsService.Application.DTOs;
using AppointmentsService.Application.Exceptions;
using AppointmentsService.Application.Interfaces;
using AppointmentsService.Domain.Entities;

namespace AppointmentsService.Application.Services;

public class ScheduleAppService
{
    private readonly IScheduleRepository _repo;

    public ScheduleAppService(IScheduleRepository repo) => _repo = repo;

    // ── ClinicSettings ────────────────────────────────────────────────────────

    public async Task<ClinicSettingsResponse> GetClinicSettingsAsync(CancellationToken ct)
    {
        var s = await _repo.GetClinicSettingsAsync(ct);
        return MapSettings(s);
    }

    public async Task<ClinicSettingsResponse> UpdateClinicSettingsAsync(
        UpdateClinicSettingsRequest req, CancellationToken ct)
    {
        if (!TimeOnly.TryParse(req.StartTime, out var start) ||
            !TimeOnly.TryParse(req.EndTime,   out var end))
            throw new ValidationException("Formato de hora inválido. Use HH:mm.");

        var days = ParseDays(req.WorkDays);
        if (!days.Any())
            throw new ValidationException("Debe haber al menos un día hábil.");

        var settings = await _repo.GetClinicSettingsAsync(ct);
        settings.Update(start, end, days);
        await _repo.SaveClinicSettingsAsync(settings, ct);
        return MapSettings(settings);
    }

    // ── VeterinarianSchedule ──────────────────────────────────────────────────

    public async Task<IReadOnlyList<VeterinarianScheduleResponse>> GetVetSchedulesAsync(
        Guid vetId, CancellationToken ct)
    {
        var schedules = await _repo.GetSchedulesForVetAsync(vetId, ct);
        return schedules.Select(MapSchedule).ToList();
    }

    public async Task<VeterinarianScheduleResponse> UpsertVetScheduleAsync(
        Guid vetId, UpsertVeterinarianScheduleRequest req, CancellationToken ct)
    {
        if (!TimeOnly.TryParse(req.StartTime, out var start) ||
            !TimeOnly.TryParse(req.EndTime,   out var end))
            throw new ValidationException("Formato de hora inválido. Use HH:mm.");

        if (!Enum.TryParse<DayOfWeek>(req.DayOfWeek, ignoreCase: true, out var day))
            throw new ValidationException($"Día inválido: {req.DayOfWeek}.");

        var schedule = VeterinarianSchedule.Create(vetId, day, start, end);
        await _repo.UpsertScheduleAsync(schedule, ct);

        var saved = await _repo.GetScheduleAsync(vetId, day, ct);
        return MapSchedule(saved!);
    }

    public async Task DeleteVetScheduleAsync(Guid vetId, string dayStr, CancellationToken ct)
    {
        if (!Enum.TryParse<DayOfWeek>(dayStr, ignoreCase: true, out var day))
            throw new ValidationException($"Día inválido: {dayStr}.");

        await _repo.DeleteScheduleAsync(vetId, day, ct);
    }

    // ── VeterinarianLeave ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<VeterinarianLeaveResponse>> GetVetLeavesAsync(
        Guid vetId, CancellationToken ct)
    {
        var leaves = await _repo.GetLeavesForVetAsync(vetId, ct);
        return leaves.Select(MapLeave).ToList();
    }

    public async Task<VeterinarianLeaveResponse> CreateVetLeaveAsync(
        Guid vetId, CreateVeterinarianLeaveRequest req, CancellationToken ct)
    {
        if (!DateOnly.TryParse(req.DateFrom, out var from) ||
            !DateOnly.TryParse(req.DateTo,   out var to))
            throw new ValidationException("Formato de fecha inválido. Use YYYY-MM-DD.");

        TimeOnly? startTime = null;
        TimeOnly? endTime   = null;

        if (req.StartTime is not null || req.EndTime is not null)
        {
            if (!TimeOnly.TryParse(req.StartTime, out var st) ||
                !TimeOnly.TryParse(req.EndTime,   out var et))
                throw new ValidationException("Formato de hora inválido. Use HH:mm.");
            startTime = st;
            endTime   = et;
        }

        var leave = VeterinarianLeave.Create(vetId, from, to, req.Reason, startTime, endTime);
        var saved = await _repo.AddLeaveAsync(leave, ct);
        return MapLeave(saved);
    }

    public async Task DeleteVetLeaveAsync(Guid leaveId, CancellationToken ct) =>
        await _repo.DeleteLeaveAsync(leaveId, ct);

    // ── Helpers de mapeo ──────────────────────────────────────────────────────

    private static ClinicSettingsResponse MapSettings(ClinicSettings s) => new()
    {
        StartTime = s.StartTime.ToString("HH:mm"),
        EndTime   = s.EndTime.ToString("HH:mm"),
        WorkDays  = s.GetWorkDays().Select(d => d.ToString()).ToList()
    };

    private static VeterinarianScheduleResponse MapSchedule(VeterinarianSchedule s) => new()
    {
        VeterinarianId = s.VeterinarianId,
        DayOfWeek      = s.DayOfWeek.ToString(),
        StartTime      = s.StartTime.ToString("HH:mm"),
        EndTime        = s.EndTime.ToString("HH:mm")
    };

    private static VeterinarianLeaveResponse MapLeave(VeterinarianLeave l) => new()
    {
        Id             = l.Id,
        VeterinarianId = l.VeterinarianId,
        DateFrom       = l.DateFrom.ToString("yyyy-MM-dd"),
        DateTo         = l.DateTo.ToString("yyyy-MM-dd"),
        StartTime      = l.StartTime?.ToString("HH:mm"),
        EndTime        = l.EndTime?.ToString("HH:mm"),
        Reason         = l.Reason
    };

    private static IEnumerable<DayOfWeek> ParseDays(IEnumerable<string> days) =>
        days.Select(d => Enum.TryParse<DayOfWeek>(d, ignoreCase: true, out var dw) ? (DayOfWeek?)dw : null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value);
}
