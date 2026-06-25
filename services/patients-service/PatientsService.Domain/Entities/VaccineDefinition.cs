using PatientsService.Domain.Exceptions;

namespace PatientsService.Domain.Entities;

public enum VaccineScheme { SingleDose, MultiDose, Annual }

public class VaccineDefinition
{
    public Guid          Id                   { get; private set; }
    public string        Name                 { get; private set; } = default!;
    public string?       Description          { get; private set; }
    public VaccineScheme Scheme               { get; private set; }
    public int           AnnualIntervalMonths { get; private set; } = 12;
    public bool          IsActive             { get; private set; } = true;
    public DateTime      CreatedAt            { get; private set; }

    public ICollection<VaccineDoseStep> DoseSteps { get; private set; } = [];

    private VaccineDefinition() { }

    public static VaccineDefinition Create(
        string name, VaccineScheme scheme, string? description = null, int annualIntervalMonths = 12)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la vacuna es obligatorio.");

        if (annualIntervalMonths < 1 || annualIntervalMonths > 36)
            throw new DomainException("El intervalo de refuerzo debe estar entre 1 y 36 meses.");

        return new VaccineDefinition
        {
            Id                   = Guid.NewGuid(),
            Name                 = name.Trim(),
            Description          = description?.Trim(),
            Scheme               = scheme,
            AnnualIntervalMonths = annualIntervalMonths,
            IsActive             = true,
            CreatedAt            = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description, int annualIntervalMonths)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la vacuna es obligatorio.");

        Name                 = name.Trim();
        Description          = description?.Trim();
        AnnualIntervalMonths = annualIntervalMonths;
    }

    public void Deactivate() => IsActive = false;
    public void Activate()   => IsActive = true;

    // Calcula la siguiente fecha de aplicación dado el número de dosis que se acaba de registrar.
    // doseNumber es la dosis que se acaba de aplicar (1-based).
    public DateOnly? CalculateNextDueDate(DateOnly administeredAt, int doseNumber)
    {
        switch (Scheme)
        {
            case VaccineScheme.SingleDose:
                return null; // sin refuerzo

            case VaccineScheme.Annual:
                return administeredAt.AddMonths(AnnualIntervalMonths);

            case VaccineScheme.MultiDose:
                var nextStep = DoseSteps
                    .OrderBy(s => s.DoseNumber)
                    .FirstOrDefault(s => s.DoseNumber == doseNumber + 1);

                if (nextStep is not null)
                    return administeredAt.AddDays(nextStep.DaysAfterPrevious);

                // Última dosis del esquema primario → pasa a refuerzo anual
                return administeredAt.AddMonths(AnnualIntervalMonths);

            default:
                return null;
        }
    }
}
