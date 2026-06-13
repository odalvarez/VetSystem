using PatientsService.Domain.Entities;

namespace PatientsService.Application.Interfaces;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<Patient> Data, int Total)> ListAsync(
        Guid? ownerId, Guid? speciesId, string? search,
        int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Patient patient, CancellationToken ct = default);
    Task UpdateAsync(Patient patient, CancellationToken ct = default);
    Task DeleteAsync(Patient patient, CancellationToken ct = default);
    Task DeleteByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<ClinicalRecord?> GetRecordAsync(Guid patientId, Guid recordId, CancellationToken ct = default);
    Task<(IEnumerable<ClinicalRecord> Data, int Total)> ListRecordsAsync(
        Guid patientId, int page, int pageSize, CancellationToken ct = default);
    Task AddRecordAsync(ClinicalRecord record, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
