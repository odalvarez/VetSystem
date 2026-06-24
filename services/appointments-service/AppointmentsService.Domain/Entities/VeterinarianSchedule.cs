using AppointmentsService.Domain.Exceptions;

namespace AppointmentsService.Domain.Entities;

// Horario personalizado de un veterinario para un día de la semana específico.
// Si no existe entrada para un día, se usa el horario global de la clínica.
public class VeterinarianSchedule
{
    public Guid      Id               { get; private set; }
    public Guid      VeterinarianId   { get; private set; }
    public DayOfWeek DayOfWeek        { get; private set; }
    public TimeOnly  StartTime        { get; private set; }
    public TimeOnly  EndTime          { get; private set; }

    private VeterinarianSchedule() { }

    public static VeterinarianSchedule Create(
        Guid veterinarianId, DayOfWeek dayOfWeek,
        TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
            throw new DomainException("La hora de cierre debe ser posterior a la de apertura.");

        return new VeterinarianSchedule
        {
            Id             = Guid.NewGuid(),
            VeterinarianId = veterinarianId,
            DayOfWeek      = dayOfWeek,
            StartTime      = startTime,
            EndTime        = endTime
        };
    }

    public void Update(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
            throw new DomainException("La hora de cierre debe ser posterior a la de apertura.");

        StartTime = startTime;
        EndTime   = endTime;
    }
}
