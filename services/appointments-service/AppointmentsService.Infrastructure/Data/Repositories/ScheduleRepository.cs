using AppointmentsService.Application.Interfaces;
using AppointmentsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppointmentsService.Infrastructure.Data.Repositories;

public class ScheduleRepository : IScheduleRepository
{
    private readonly AppointmentsDbContext _db;

    public ScheduleRepository(AppointmentsDbContext db) => _db = db;

    // ── ClinicSettings ────────────────────────────────────────────────────────

    public async Task<ClinicSettings> GetClinicSettingsAsync(CancellationToken ct)
    {
        var settings = await _db.ClinicSettings.FindAsync([1], ct);
        if (settings is not null) return settings;

        // Primera vez: inicializa con valores por defecto y persiste
        var defaults = ClinicSettings.Default();
        _db.ClinicSettings.Add(defaults);
        await _db.SaveChangesAsync(ct);
        return defaults;
    }

    public async Task SaveClinicSettingsAsync(ClinicSettings settings, CancellationToken ct)
    {
        _db.ClinicSettings.Update(settings);
        await _db.SaveChangesAsync(ct);
    }

    // ── VeterinarianSchedule ──────────────────────────────────────────────────

    public async Task<IReadOnlyList<VeterinarianSchedule>> GetSchedulesForVetAsync(
        Guid vetId, CancellationToken ct) =>
        await _db.VeterinarianSchedules
            .Where(s => s.VeterinarianId == vetId)
            .ToListAsync(ct);

    public async Task<VeterinarianSchedule?> GetScheduleAsync(
        Guid vetId, DayOfWeek day, CancellationToken ct) =>
        await _db.VeterinarianSchedules
            .FirstOrDefaultAsync(s => s.VeterinarianId == vetId && s.DayOfWeek == day, ct);

    public async Task UpsertScheduleAsync(VeterinarianSchedule schedule, CancellationToken ct)
    {
        var existing = await GetScheduleAsync(schedule.VeterinarianId, schedule.DayOfWeek, ct);
        if (existing is null)
            _db.VeterinarianSchedules.Add(schedule);
        else
            existing.Update(schedule.StartTime, schedule.EndTime);

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteScheduleAsync(Guid vetId, DayOfWeek day, CancellationToken ct)
    {
        var s = await GetScheduleAsync(vetId, day, ct);
        if (s is not null) _db.VeterinarianSchedules.Remove(s);
        await _db.SaveChangesAsync(ct);
    }

    // ── VeterinarianLeave ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<VeterinarianLeave>> GetLeavesForVetAsync(
        Guid vetId, CancellationToken ct) =>
        await _db.VeterinarianLeaves
            .Where(l => l.VeterinarianId == vetId)
            .OrderBy(l => l.DateFrom)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<VeterinarianLeave>> GetLeavesOnDateAsync(
        Guid vetId, DateOnly date, CancellationToken ct) =>
        await _db.VeterinarianLeaves
            .Where(l => l.VeterinarianId == vetId && l.DateFrom <= date && l.DateTo >= date)
            .ToListAsync(ct);

    public async Task<VeterinarianLeave> AddLeaveAsync(VeterinarianLeave leave, CancellationToken ct)
    {
        _db.VeterinarianLeaves.Add(leave);
        await _db.SaveChangesAsync(ct);
        return leave;
    }

    public async Task DeleteLeaveAsync(Guid leaveId, CancellationToken ct)
    {
        var leave = await _db.VeterinarianLeaves.FindAsync([leaveId], ct);
        if (leave is not null) _db.VeterinarianLeaves.Remove(leave);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
