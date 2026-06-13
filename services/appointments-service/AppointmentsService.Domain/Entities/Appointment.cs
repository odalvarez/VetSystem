using AppointmentsService.Domain.Enums;
using AppointmentsService.Domain.Exceptions;

namespace AppointmentsService.Domain.Entities;

public class Appointment
{
    public Guid             Id               { get; private set; }
    public Guid             PatientId        { get; private set; }
    public string           PatientName      { get; private set; } = default!;
    public Guid             OwnerId          { get; private set; }
    public string           OwnerName        { get; private set; } = default!;
    public string           OwnerPhone       { get; private set; } = default!;
    public Guid             VeterinarianId   { get; private set; }
    public string           VeterinarianName { get; private set; } = default!;
    public DateTime         ScheduledAt      { get; private set; }
    public int              DurationMinutes  { get; private set; }
    public string           Reason           { get; private set; } = default!;
    public AppointmentStatus Status          { get; private set; }
    public string?          Notes            { get; private set; }
    public DateTime         CreatedAt        { get; private set; }
    public DateTime         UpdatedAt        { get; private set; }

    private static readonly HashSet<(AppointmentStatus From, AppointmentStatus To)> AllowedTransitions = new()
    {
        (AppointmentStatus.Scheduled,  AppointmentStatus.Completed),
        (AppointmentStatus.Scheduled,  AppointmentStatus.Cancelled),
        (AppointmentStatus.Scheduled,  AppointmentStatus.NoShow),
    };

    private Appointment() { }

    public static Appointment Create(
        Guid patientId, string patientName,
        Guid ownerId, string ownerName, string ownerPhone,
        Guid veterinarianId, string veterinarianName,
        DateTime scheduledAt, int durationMinutes,
        string reason, string? notes)
    {
        if (durationMinutes < 10 || durationMinutes > 480)
            throw new DomainException("La duración debe estar entre 10 y 480 minutos.");

        if (scheduledAt < DateTime.UtcNow)
            throw new DomainException("No se puede agendar una cita en el pasado.");

        return new Appointment
        {
            Id               = Guid.NewGuid(),
            PatientId        = patientId,
            PatientName      = patientName,
            OwnerId          = ownerId,
            OwnerName        = ownerName,
            OwnerPhone       = ownerPhone,
            VeterinarianId   = veterinarianId,
            VeterinarianName = veterinarianName,
            ScheduledAt      = scheduledAt,
            DurationMinutes  = durationMinutes,
            Reason           = reason.Trim(),
            Status           = AppointmentStatus.Scheduled,
            Notes            = notes?.Trim(),
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow
        };
    }

    public void Update(DateTime scheduledAt, int durationMinutes, string reason, string? notes)
    {
        if (Status != AppointmentStatus.Scheduled)
            throw new DomainException("Solo se pueden modificar citas en estado 'scheduled'.");

        if (scheduledAt < DateTime.UtcNow)
            throw new DomainException("No se puede mover la cita a una fecha pasada.");

        ScheduledAt     = scheduledAt;
        DurationMinutes = durationMinutes;
        Reason          = reason.Trim();
        Notes           = notes?.Trim();
        UpdatedAt       = DateTime.UtcNow;
    }

    public void ChangeStatus(AppointmentStatus newStatus)
    {
        if (!AllowedTransitions.Contains((Status, newStatus)))
            throw new DomainException($"Transición de '{Status}' a '{newStatus}' no permitida.");

        Status    = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public DateTime EndsAt => ScheduledAt.AddMinutes(DurationMinutes);
}
