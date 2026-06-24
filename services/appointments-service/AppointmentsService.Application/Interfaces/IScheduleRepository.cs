using AppointmentsService.Domain.Entities;

namespace AppointmentsService.Application.Interfaces;

public interface IScheduleRepository
{
    // ── ClinicSettings ────────────────────────────────────────────────────────
    Task<ClinicSettings> GetClinicSettingsAsync(CancellationToken ct);
    Task SaveClinicSettingsAsync(ClinicSettings settings, CancellationToken ct);

    // ── VeterinarianSchedule ──────────────────────────────────────────────────
    Task<IReadOnlyList<VeterinarianSchedule>> GetSchedulesForVetAsync(Guid vetId, CancellationToken ct);
    Task<VeterinarianSchedule?> GetScheduleAsync(Guid vetId, DayOfWeek day, CancellationToken ct);
    Task UpsertScheduleAsync(VeterinarianSchedule schedule, CancellationToken ct);
    Task DeleteScheduleAsync(Guid vetId, DayOfWeek day, CancellationToken ct);

    // ── VeterinarianLeave ─────────────────────────────────────────────────────
    Task<IReadOnlyList<VeterinarianLeave>> GetLeavesForVetAsync(Guid vetId, CancellationToken ct);
    Task<IReadOnlyList<VeterinarianLeave>> GetLeavesOnDateAsync(Guid vetId, DateOnly date, CancellationToken ct);
    Task<VeterinarianLeave> AddLeaveAsync(VeterinarianLeave leave, CancellationToken ct);
    Task DeleteLeaveAsync(Guid leaveId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
