namespace PatientsService.Domain.Entities;

// Un paso del esquema de primovacunación.
// Ejemplo Distemper/Parvo: Dosis1(ref), Dosis2(+28d), Dosis3(+28d) → luego anual.
public class VaccineDoseStep
{
    public int  Id                   { get; private set; }
    public Guid VaccineDefinitionId  { get; private set; }
    public int  DoseNumber           { get; private set; }
    public int  DaysAfterPrevious    { get; private set; }

    private VaccineDoseStep() { }

    public static VaccineDoseStep Create(Guid vaccineDefinitionId, int doseNumber, int daysAfterPrevious) =>
        new() { VaccineDefinitionId = vaccineDefinitionId, DoseNumber = doseNumber, DaysAfterPrevious = daysAfterPrevious };
}
