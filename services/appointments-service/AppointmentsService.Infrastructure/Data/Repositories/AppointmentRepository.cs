using AppointmentsService.Application.Interfaces;
using AppointmentsService.Domain.Entities;
using AppointmentsService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AppointmentsService.Infrastructure.Data.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppointmentsDbContext _db;

    public AppointmentRepository(AppointmentsDbContext db) => _db = db;

    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<(IEnumerable<Appointment> Data, int Total)> ListAsync(
        Guid? ownerId, AppointmentStatus? status,
        DateTime? from, DateTime? to,
        Guid? veterinarianId, Guid? patientId,
        int page, int pageSize, CancellationToken ct)
    {
        var q = _db.Appointments.AsQueryable();

        if (ownerId.HasValue)        q = q.Where(a => a.OwnerId == ownerId.Value);
        if (status.HasValue)         q = q.Where(a => a.Status == status.Value);
        if (from.HasValue)           q = q.Where(a => a.ScheduledAt >= from.Value);
        if (to.HasValue)             q = q.Where(a => a.ScheduledAt <= to.Value);
        if (veterinarianId.HasValue) q = q.Where(a => a.VeterinarianId == veterinarianId.Value);
        if (patientId.HasValue)      q = q.Where(a => a.PatientId == patientId.Value);

        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(a => a.ScheduledAt)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync(ct);
        return (data, total);
    }

    public async Task<IEnumerable<Appointment>> GetOverlappingAsync(
        Guid veterinarianId, DateTime start, DateTime end, Guid? excludeId, CancellationToken ct)
    {
        // Dos intervalos solapan si uno empieza antes de que el otro termine y termina después de que el otro empiece
        var q = _db.Appointments.Where(a =>
            a.VeterinarianId == veterinarianId &&
            a.Status != AppointmentStatus.Cancelled &&
            a.ScheduledAt < end &&
            a.ScheduledAt.AddMinutes(a.DurationMinutes) > start);

        if (excludeId.HasValue)
            q = q.Where(a => a.Id != excludeId.Value);

        return await q.ToListAsync(ct);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken ct) =>
        await _db.Appointments.AddAsync(appointment, ct);

    public Task UpdateAsync(Appointment appointment, CancellationToken ct)
    {
        _db.Appointments.Update(appointment);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) =>
        _db.SaveChangesAsync(ct);
}
