using AppointmentsService.Domain.Entities;
using AppointmentsService.Domain.Enums;

namespace AppointmentsService.Application.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<Appointment> Data, int Total)> ListAsync(
        Guid? ownerId, AppointmentStatus? status,
        DateTime? from, DateTime? to,
        Guid? veterinarianId, Guid? patientId,
        int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Appointment>> GetOverlappingAsync(
        Guid veterinarianId, DateTime start, DateTime end,
        Guid? excludeId, CancellationToken ct = default);
    Task AddAsync(Appointment appointment, CancellationToken ct = default);
    Task UpdateAsync(Appointment appointment, CancellationToken ct = default);
    Task<IEnumerable<Appointment>> GetScheduledForDateAsync(DateOnly date, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
