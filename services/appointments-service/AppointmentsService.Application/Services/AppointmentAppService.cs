using AppointmentsService.Application.DTOs;
using AppointmentsService.Application.Exceptions;
using AppointmentsService.Application.Interfaces;
using AppointmentsService.Domain.Entities;
using AppointmentsService.Domain.Enums;

namespace AppointmentsService.Application.Services;

public class AppointmentAppService
{
    private readonly IAppointmentRepository _repo;
    private readonly INotificationClient    _notifications;

    // Horario de atención: 08:00 a 18:00 en la zona horaria del servidor
    private static readonly TimeOnly WorkStart = new(8, 0);
    private static readonly TimeOnly WorkEnd   = new(18, 0);

    public AppointmentAppService(
        IAppointmentRepository repo,
        INotificationClient notifications)
    {
        _repo          = repo;
        _notifications = notifications;
    }

    public async Task<AppointmentResponse> CreateAsync(
        CreateAppointmentRequest req, Guid callerId, bool isOwner, CancellationToken ct)
    {
        // owner solo puede crear citas para sí mismo
        if (isOwner && req.OwnerId != callerId)
            throw new ForbiddenException("Un propietario solo puede agendar citas para sus propias mascotas.");

        var endTime = req.ScheduledAt.AddMinutes(req.DurationMinutes);
        var overlaps = await _repo.GetOverlappingAsync(req.VeterinarianId, req.ScheduledAt, endTime, null, ct);
        if (overlaps.Any())
            throw new ConflictException("El veterinario ya tiene una cita en ese intervalo.");

        var appointment = Appointment.Create(
            req.PatientId, req.PatientName,
            req.OwnerId, req.OwnerName, req.OwnerPhone,
            req.VeterinarianId, req.VeterinarianName,
            req.ScheduledAt, req.DurationMinutes, req.Reason, req.Notes);

        await _repo.AddAsync(appointment, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(appointment);
    }

    public async Task<PagedResponse<AppointmentResponse>> ListAsync(
        Guid callerId, bool isOwner,
        AppointmentStatus? status, DateTime? from, DateTime? to,
        Guid? veterinarianId, Guid? patientId,
        int page, int pageSize, CancellationToken ct)
    {
        Guid? ownerFilter = isOwner ? callerId : null;
        var (data, total) = await _repo.ListAsync(ownerFilter, status, from, to, veterinarianId, patientId, page, pageSize, ct);
        return new PagedResponse<AppointmentResponse>
        {
            Items = data.Select(Map), TotalCount = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<AppointmentResponse> GetAsync(Guid id, Guid callerId, bool isOwner, CancellationToken ct)
    {
        var appt = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Cita no encontrada.");

        if (isOwner && appt.OwnerId != callerId)
            throw new ForbiddenException("Sin permiso sobre esta cita.");

        return Map(appt);
    }

    public async Task<AppointmentResponse> UpdateAsync(
        Guid id, UpdateAppointmentRequest req, Guid callerId, bool isOwner, CancellationToken ct)
    {
        var appt = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Cita no encontrada.");

        if (isOwner && appt.OwnerId != callerId)
            throw new ForbiddenException("Sin permiso sobre esta cita.");

        var endTime  = req.ScheduledAt.AddMinutes(req.DurationMinutes);
        var overlaps = await _repo.GetOverlappingAsync(appt.VeterinarianId, req.ScheduledAt, endTime, id, ct);
        if (overlaps.Any())
            throw new ConflictException("El veterinario ya tiene una cita en ese intervalo.");

        appt.Update(req.ScheduledAt, req.DurationMinutes, req.Reason, req.Notes);
        await _repo.UpdateAsync(appt, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(appt);
    }

    public async Task<AppointmentResponse> ChangeStatusAsync(
        Guid id, string statusStr, CancellationToken ct)
    {
        var appt = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Cita no encontrada.");

        if (!Enum.TryParse<AppointmentStatus>(statusStr, ignoreCase: true, out var newStatus))
            throw new ValidationException("Estado no válido.");

        appt.ChangeStatus(newStatus);
        await _repo.UpdateAsync(appt, ct);
        await _repo.SaveChangesAsync(ct);

        // Al confirmar la cita programamos el recordatorio 24h antes.
        // Lo hacemos DESPUÉS de guardar para no perder el cambio de estado si falla.
        // El cliente absorbe cualquier excepción del notifications-service internamente.
        if (newStatus == AppointmentStatus.Confirmed)
        {
            await _notifications.ScheduleReminderAsync(
                appointmentId: appt.Id,
                patientName:   appt.PatientName,
                ownerName:     appt.OwnerName,
                ownerPhone:    appt.OwnerPhone,
                ownerEmail:    "",   // el email del owner no llega en el appointments-service;
                                     // el recordatorio enviará solo por WhatsApp en este caso
                scheduledAt:   appt.ScheduledAt,
                ct:            ct);
        }

        return Map(appt);
    }

    public async Task CancelAsync(Guid id, Guid callerId, bool isOwner, CancellationToken ct)
    {
        var appt = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Cita no encontrada.");

        if (isOwner)
        {
            if (appt.OwnerId != callerId)
                throw new ForbiddenException("Sin permiso sobre esta cita.");

            if (appt.Status != AppointmentStatus.Scheduled)
                throw new ValidationException("Solo se pueden cancelar citas en estado 'scheduled'.");
        }

        appt.ChangeStatus(AppointmentStatus.Cancelled);
        await _repo.UpdateAsync(appt, ct);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task<AvailabilityResponse> GetAvailabilityAsync(
        Guid veterinarianId, DateOnly date, int durationMinutes, CancellationToken ct)
    {
        var dayStart = date.ToDateTime(WorkStart, DateTimeKind.Utc);
        var dayEnd   = date.ToDateTime(WorkEnd,   DateTimeKind.Utc);

        var existing = await _repo.GetOverlappingAsync(veterinarianId, dayStart, dayEnd, null, ct);
        var busySlots = existing
            .Select(a => (a.ScheduledAt, a.EndsAt))
            .OrderBy(s => s.ScheduledAt)
            .ToList();

        var slots  = new List<TimeSlot>();
        var cursor = dayStart;

        foreach (var (start, end) in busySlots)
        {
            while (cursor.AddMinutes(durationMinutes) <= start)
            {
                slots.Add(new TimeSlot { Start = cursor, End = cursor.AddMinutes(durationMinutes) });
                cursor = cursor.AddMinutes(durationMinutes);
            }
            cursor = end > cursor ? end : cursor;
        }

        while (cursor.AddMinutes(durationMinutes) <= dayEnd)
        {
            slots.Add(new TimeSlot { Start = cursor, End = cursor.AddMinutes(durationMinutes) });
            cursor = cursor.AddMinutes(durationMinutes);
        }

        return new AvailabilityResponse
        {
            Date           = date.ToString("yyyy-MM-dd"),
            VeterinarianId = veterinarianId,
            AvailableSlots = slots
        };
    }

    private static AppointmentResponse Map(Appointment a) => new()
    {
        Id               = a.Id,
        PatientId        = a.PatientId,
        PatientName      = a.PatientName,
        OwnerId          = a.OwnerId,
        OwnerName        = a.OwnerName,
        OwnerPhone       = a.OwnerPhone,
        VeterinarianId   = a.VeterinarianId,
        VeterinarianName = a.VeterinarianName,
        ScheduledAt      = a.ScheduledAt,
        DurationMinutes  = a.DurationMinutes,
        Reason           = a.Reason,
        Status           = a.Status.ToString().ToLowerInvariant(),
        Notes            = a.Notes,
        CreatedAt        = a.CreatedAt,
        UpdatedAt        = a.UpdatedAt
    };
}
