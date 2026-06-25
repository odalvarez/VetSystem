using Microsoft.EntityFrameworkCore;
using PatientsService.Application.Interfaces;
using PatientsService.Domain.Entities;

namespace PatientsService.Infrastructure.Data.Repositories;

public class VaccinationRepository(PatientsDbContext db) : IVaccinationRepository
{
    // ── VaccineDefinition ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<VaccineDefinition>> ListDefinitionsAsync(CancellationToken ct) =>
        await db.VaccineDefinitions
            .Include(v => v.DoseSteps.OrderBy(s => s.DoseNumber))
            .OrderBy(v => v.Name)
            .ToListAsync(ct);

    public async Task<VaccineDefinition?> GetDefinitionAsync(Guid id, CancellationToken ct) =>
        await db.VaccineDefinitions
            .Include(v => v.DoseSteps.OrderBy(s => s.DoseNumber))
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task AddDefinitionAsync(VaccineDefinition definition, CancellationToken ct) =>
        await db.VaccineDefinitions.AddAsync(definition, ct);

    public async Task AddDoseStepAsync(VaccineDoseStep step, CancellationToken ct) =>
        await db.VaccineDoseSteps.AddAsync(step, ct);

    public async Task RemoveDoseStepsAsync(Guid vaccineDefinitionId, CancellationToken ct)
    {
        var steps = await db.VaccineDoseSteps
            .Where(s => s.VaccineDefinitionId == vaccineDefinitionId)
            .ToListAsync(ct);
        db.VaccineDoseSteps.RemoveRange(steps);
    }

    // ── VaccinationRecord ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<VaccinationRecord>> ListByPatientAsync(Guid patientId, CancellationToken ct) =>
        await db.VaccinationRecords
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.AdministeredAt)
            .ToListAsync(ct);

    public async Task<VaccinationRecord?> GetRecordAsync(Guid id, CancellationToken ct) =>
        await db.VaccinationRecords.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<int> GetNextDoseNumberAsync(Guid patientId, Guid vaccineDefinitionId, CancellationToken ct)
    {
        var last = await db.VaccinationRecords
            .Where(r => r.PatientId == patientId && r.VaccineDefinitionId == vaccineDefinitionId)
            .OrderByDescending(r => r.DoseNumber)
            .Select(r => (int?)r.DoseNumber)
            .FirstOrDefaultAsync(ct);
        return (last ?? 0) + 1;
    }

    public async Task AddRecordAsync(VaccinationRecord record, CancellationToken ct) =>
        await db.VaccinationRecords.AddAsync(record, ct);

    public async Task DeleteRecordAsync(Guid id, CancellationToken ct)
    {
        var record = await db.VaccinationRecords.FindAsync([id], ct);
        if (record is not null)
            db.VaccinationRecords.Remove(record);
    }

    // ── Worker de recordatorios ───────────────────────────────────────────────

    public async Task<IReadOnlyList<VaccinationRecord>> GetPendingReminder7Async(DateOnly targetDate, CancellationToken ct) =>
        await db.VaccinationRecords
            .Where(r => r.NextDueDate == targetDate && r.Reminder7SentAt == null)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<VaccinationRecord>> GetPendingReminder2Async(DateOnly targetDate, CancellationToken ct) =>
        await db.VaccinationRecords
            .Where(r => r.NextDueDate == targetDate && r.Reminder2SentAt == null)
            .ToListAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct) =>
        await db.SaveChangesAsync(ct);
}
