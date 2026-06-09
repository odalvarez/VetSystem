using PatientsService.Domain.Entities;

namespace PatientsService.Application.Interfaces;

public interface IConsultationLogRepository
{
    Task<ConsultationLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<ConsultationLog> Data, int Total)> ListByPatientAsync(
        Guid patientId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(ConsultationLog log, CancellationToken ct = default);
    Task UpdateAsync(ConsultationLog log, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
