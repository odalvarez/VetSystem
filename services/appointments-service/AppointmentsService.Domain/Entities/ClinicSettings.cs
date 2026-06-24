namespace AppointmentsService.Domain.Entities;

public class ClinicSettings
{
    public int      Id        { get; private set; } = 1;
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime   { get; private set; }
    // Días de la semana habilitados, almacenados como lista separada por comas: "Monday,Tuesday,..."
    public string   WorkDays  { get; private set; } = default!;

    private ClinicSettings() { }

    public static ClinicSettings Default() => new()
    {
        StartTime = new TimeOnly(8, 0),
        EndTime   = new TimeOnly(20, 0),
        WorkDays  = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday"
    };

    public void Update(TimeOnly startTime, TimeOnly endTime, IEnumerable<DayOfWeek> workDays)
    {
        if (endTime <= startTime)
            throw new Domain.Exceptions.DomainException("La hora de cierre debe ser posterior a la de apertura.");

        StartTime = startTime;
        EndTime   = endTime;
        WorkDays  = string.Join(",", workDays.Distinct().OrderBy(d => d).Select(d => d.ToString()));
    }

    public IReadOnlyList<DayOfWeek> GetWorkDays() =>
        WorkDays.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Enum.Parse<DayOfWeek>(s))
                .ToList();
}
