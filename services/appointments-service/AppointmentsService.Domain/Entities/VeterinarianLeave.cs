using AppointmentsService.Domain.Exceptions;

namespace AppointmentsService.Domain.Entities;

// Ausencia temporal de un veterinario (vacaciones, permiso, enfermedad, etc.).
// Durante el rango de fechas el vet no aparece disponible.
public class VeterinarianLeave
{
    public Guid     Id             { get; private set; }
    public Guid     VeterinarianId { get; private set; }
    public DateOnly DateFrom       { get; private set; }
    public DateOnly DateTo         { get; private set; }
    public string   Reason         { get; private set; } = default!;

    private VeterinarianLeave() { }

    public static VeterinarianLeave Create(
        Guid veterinarianId, DateOnly dateFrom, DateOnly dateTo, string reason)
    {
        if (dateTo < dateFrom)
            throw new DomainException("La fecha de fin debe ser igual o posterior a la de inicio.");

        return new VeterinarianLeave
        {
            Id             = Guid.NewGuid(),
            VeterinarianId = veterinarianId,
            DateFrom       = dateFrom,
            DateTo         = dateTo,
            Reason         = reason.Trim()
        };
    }

    public bool CoversDate(DateOnly date) => date >= DateFrom && date <= DateTo;
}
