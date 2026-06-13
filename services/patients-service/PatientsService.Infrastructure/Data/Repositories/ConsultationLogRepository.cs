using Microsoft.EntityFrameworkCore;
using PatientsService.Application.Interfaces;
using PatientsService.Domain.Entities;

namespace PatientsService.Infrastructure.Data.Repositories;

public class ConsultationLogRepository : IConsultationLogRepository
{
    private readonly PatientsDbContext _db;

    public ConsultationLogRepository(PatientsDbContext db) => _db = db;

    public Task<ConsultationLog?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.ConsultationLogs.FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<ConsultationLog?> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct) =>
        _db.ConsultationLogs.FirstOrDefaultAsync(l => l.AppointmentId == appointmentId, ct);

    public Task<bool> ExistsByAppointmentAsync(Guid appointmentId, CancellationToken ct) =>
        _db.ConsultationLogs.AnyAsync(l => l.AppointmentId == appointmentId, ct);

    public async Task<(IEnumerable<ConsultationLog> Data, int Total)> ListByPatientAsync(
        Guid patientId, int page, int pageSize, CancellationToken ct)
    {
        var q     = _db.ConsultationLogs.Where(l => l.PatientId == patientId);
        var total = await q.CountAsync(ct);
        var data  = await q.OrderByDescending(l => l.OpenedAt)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync(ct);
        return (data, total);
    }

    public async Task AddAsync(ConsultationLog log, CancellationToken ct) =>
        await _db.ConsultationLogs.AddAsync(log, ct);

    public Task UpdateAsync(ConsultationLog log, CancellationToken ct)
    {
        _db.ConsultationLogs.Update(log);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) =>
        _db.SaveChangesAsync(ct);
}
