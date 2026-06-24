using AppointmentsService.Domain.Exceptions;

namespace AppointmentsService.Domain.Entities;

// Ausencia temporal de un veterinario (vacaciones, permiso, enfermedad, etc.).
// Si StartTime/EndTime son null → ausencia de día completo.
// Si tienen valor → el vet solo está bloqueado en ese rango horario.
public class VeterinarianLeave
{
    public Guid      Id             { get; private set; }
    public Guid      VeterinarianId { get; private set; }
    public DateOnly  DateFrom       { get; private set; }
    public DateOnly  DateTo         { get; private set; }
    public TimeOnly? StartTime      { get; private set; }
    public TimeOnly? EndTime        { get; private set; }
    public string    Reason         { get; private set; } = default!;

    private VeterinarianLeave() { }

    public static VeterinarianLeave Create(
        Guid veterinarianId, DateOnly dateFrom, DateOnly dateTo, string reason,
        TimeOnly? startTime = null, TimeOnly? endTime = null)
    {
        if (dateTo < dateFrom)
            throw new DomainException("La fecha de fin debe ser igual o posterior a la de inicio.");

        if (startTime.HasValue != endTime.HasValue)
            throw new DomainException("Debe indicar tanto la hora de inicio como la de fin, o ninguna.");

        if (startTime.HasValue && endTime <= startTime)
            throw new DomainException("La hora de fin debe ser posterior a la de inicio.");

        // Ausencia parcial solo tiene sentido en un único día
        if (startTime.HasValue && dateFrom != dateTo)
            throw new DomainException("Las ausencias con rango horario solo pueden aplicar a un mismo día.");

        return new VeterinarianLeave
        {
            Id             = Guid.NewGuid(),
            VeterinarianId = veterinarianId,
            DateFrom       = dateFrom,
            DateTo         = dateTo,
            StartTime      = startTime,
            EndTime        = endTime,
            Reason         = reason.Trim()
        };
    }

    public bool IsFullDay => !StartTime.HasValue;

    public bool CoversDate(DateOnly date) => date >= DateFrom && date <= DateTo;

    // Determina si esta ausencia bloquea un slot que va de slotStart a slotEnd en una fecha dada.
    public bool BlocksSlot(DateOnly date, TimeOnly slotStart, TimeOnly slotEnd)
    {
        if (!CoversDate(date)) return false;
        if (IsFullDay) return true;
        // Ausencia parcial: bloquea si hay solapamiento con el slot
        return slotStart < EndTime!.Value && slotEnd > StartTime!.Value;
    }
}
