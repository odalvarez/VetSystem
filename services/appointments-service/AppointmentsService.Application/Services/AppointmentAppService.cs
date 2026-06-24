using AppointmentsService.Application.DTOs;
using AppointmentsService.Application.Exceptions;
using AppointmentsService.Application.Interfaces;
using AppointmentsService.Domain.Entities;
using AppointmentsService.Domain.Enums;

namespace AppointmentsService.Application.Services;

public class AppointmentAppService
{
    private readonly IAppointmentRepository _repo;
    private readonly IScheduleRepository    _schedules;
    private readonly INotificationClient    _notifications;

    public AppointmentAppService(
        IAppointmentRepository repo,
        IScheduleRepository    schedules,
        INotificationClient    notifications)
    {
        _repo          = repo;
        _schedules     = schedules;
        _notifications = notifications;
    }

    public async Task<AppointmentResponse> CreateAsync(
        CreateAppointmentRequest req, Guid callerId, bool isOwner, CancellationToken ct)
    {
        if (isOwner && req.OwnerId != callerId)
            throw new ForbiddenException("Un propietario solo puede agendar citas para sus propias mascotas.");

        var date    = DateOnly.FromDateTime(req.ScheduledAt);
        var endTime = req.ScheduledAt.AddMinutes(req.DurationMinutes);

        var (start, end, available) = await ResolveScheduleAsync(req.VeterinarianId, date, ct);
        if (!available)
            throw new ValidationException("El veterinario no está disponible ese día (ausencia o día no laborable).");

        var apptStart = TimeOnly.FromDateTime(req.ScheduledAt);
        var apptEnd   = TimeOnly.FromDateTime(endTime);
        if (apptStart < start || apptEnd > end)
            throw new ValidationException(
                $"Las citas deben estar dentro del horario de atención ({start:HH\\:mm}–{end:HH\\:mm}).");

        var overlaps = await _repo.GetOverlappingAsync(req.VeterinarianId, req.ScheduledAt, endTime, null, ct);
        if (overlaps.Any())
            throw new ConflictException("El veterinario ya tiene una cita en ese intervalo.");

        var appointment = Appointment.Create(
            req.PatientId, req.PatientName,
            req.OwnerId, req.OwnerName, req.OwnerPhone, req.OwnerEmail,
            req.VeterinarianId, req.VeterinarianName,
            req.ScheduledAt, req.DurationMinutes, req.Reason, req.Notes);

        await _repo.AddAsync(appointment, ct);
        await _repo.SaveChangesAsync(ct);

        await _notifications.SendConfirmationAsync(
            appointmentId:    appointment.Id,
            patientName:      appointment.PatientName,
            ownerName:        appointment.OwnerName,
            ownerPhone:       appointment.OwnerPhone,
            ownerEmail:       appointment.OwnerEmail,
            veterinarianName: appointment.VeterinarianName,
            scheduledAt:      appointment.ScheduledAt,
            durationMinutes:  appointment.DurationMinutes,
            reason:           appointment.Reason,
            ct:               ct);

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

        var date    = DateOnly.FromDateTime(req.ScheduledAt);
        var endTime = req.ScheduledAt.AddMinutes(req.DurationMinutes);

        var (start, end, available) = await ResolveScheduleAsync(appt.VeterinarianId, date, ct);
        if (!available)
            throw new ValidationException("El veterinario no está disponible ese día.");

        var apptStart = TimeOnly.FromDateTime(req.ScheduledAt);
        var apptEnd   = TimeOnly.FromDateTime(endTime);
        if (apptStart < start || apptEnd > end)
            throw new ValidationException(
                $"Las citas deben estar dentro del horario de atención ({start:HH\\:mm}–{end:HH\\:mm}).");

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
        var (workStart, workEnd, available) = await ResolveScheduleAsync(veterinarianId, date, ct);

        if (!available)
            return new AvailabilityResponse
            {
                Date           = date.ToString("yyyy-MM-dd"),
                VeterinarianId = veterinarianId,
                AvailableSlots = []
            };

        var dayStart = date.ToDateTime(workStart, DateTimeKind.Utc);
        var dayEnd   = date.ToDateTime(workEnd,   DateTimeKind.Utc);

        var existing  = await _repo.GetOverlappingAsync(veterinarianId, dayStart, dayEnd, null, ct);
        var partialLeaves = (await _schedules.GetLeavesOnDateAsync(veterinarianId, date, ct))
            .Where(l => !l.IsFullDay)
            .ToList();

        // Combina citas existentes y ausencias parciales como intervalos bloqueados
        var blockedIntervals = existing
            .Select(a => (a.ScheduledAt, a.EndsAt))
            .Concat(partialLeaves.Select(l => (
                date.ToDateTime(l.StartTime!.Value, DateTimeKind.Utc),
                date.ToDateTime(l.EndTime!.Value,   DateTimeKind.Utc))))
            .OrderBy(s => s.Item1)
            .ToList();

        var slots  = new List<TimeSlot>();
        var cursor = dayStart;

        foreach (var (blockStart, blockEnd) in blockedIntervals)
        {
            while (cursor.AddMinutes(durationMinutes) <= blockStart)
            {
                slots.Add(new TimeSlot { Start = cursor, End = cursor.AddMinutes(durationMinutes) });
                cursor = cursor.AddMinutes(durationMinutes);
            }
            cursor = blockEnd > cursor ? blockEnd : cursor;
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

    // ── Resolución de horario efectivo ────────────────────────────────────────

    // Devuelve (startTime, endTime, isAvailable).
    // Prioridad: ausencia día completo → horario personal del vet → horario global de la clínica.
    // Las ausencias parciales no bloquean el día entero; se filtran slot a slot en GetAvailabilityAsync.
    private async Task<(TimeOnly Start, TimeOnly End, bool Available)> ResolveScheduleAsync(
        Guid vetId, DateOnly date, CancellationToken ct)
    {
        // 1. ¿Tiene alguna ausencia de día completo?
        var leaves = await _schedules.GetLeavesOnDateAsync(vetId, date, ct);
        if (leaves.Any(l => l.IsFullDay))
            return (default, default, false);

        // 2. ¿Tiene horario personalizado para ese día?
        var personal = await _schedules.GetScheduleAsync(vetId, date.DayOfWeek, ct);
        if (personal is not null)
            return (personal.StartTime, personal.EndTime, true);

        // 3. Horario global de la clínica
        var clinic = await _schedules.GetClinicSettingsAsync(ct);
        if (!clinic.GetWorkDays().Contains(date.DayOfWeek))
            return (default, default, false);

        return (clinic.StartTime, clinic.EndTime, true);
    }

    private static AppointmentResponse Map(Appointment a) => new()
    {
        Id               = a.Id,
        PatientId        = a.PatientId,
        PatientName      = a.PatientName,
        OwnerId          = a.OwnerId,
        OwnerName        = a.OwnerName,
        OwnerPhone       = a.OwnerPhone,
        OwnerEmail       = a.OwnerEmail,
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
