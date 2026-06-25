using PatientsService.Domain.Entities;

namespace PatientsService.Application.Interfaces;

public interface IVaccinationRepository
{
    // ── VaccineDefinition ─────────────────────────────────────────────────────
    Task<IReadOnlyList<VaccineDefinition>> ListDefinitionsAsync(CancellationToken ct);
    Task<VaccineDefinition?> GetDefinitionAsync(Guid id, CancellationToken ct);
    Task AddDefinitionAsync(VaccineDefinition definition, CancellationToken ct);
    Task AddDoseStepAsync(VaccineDoseStep step, CancellationToken ct);
    Task RemoveDoseStepsAsync(Guid vaccineDefinitionId, CancellationToken ct);

    // ── VaccinationRecord ─────────────────────────────────────────────────────
    Task<IReadOnlyList<VaccinationRecord>> ListByPatientAsync(Guid patientId, CancellationToken ct);
    Task<VaccinationRecord?> GetRecordAsync(Guid id, CancellationToken ct);
    Task<int> GetNextDoseNumberAsync(Guid patientId, Guid vaccineDefinitionId, CancellationToken ct);
    Task AddRecordAsync(VaccinationRecord record, CancellationToken ct);
    Task DeleteRecordAsync(Guid id, CancellationToken ct);

    // ── Worker de recordatorios ───────────────────────────────────────────────
    Task<IReadOnlyList<VaccinationRecord>> GetPendingReminder7Async(DateOnly targetDate, CancellationToken ct);
    Task<IReadOnlyList<VaccinationRecord>> GetPendingReminder2Async(DateOnly targetDate, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
