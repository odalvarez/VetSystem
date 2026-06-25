namespace VetSystem.Frontend.Models;

public record VaccineDoseStepDto(int DoseNumber, int DaysAfterPrevious);

public record VaccineDefinitionResponse(
    Guid   Id,
    string Name,
    string? Description,
    string Scheme,
    int    AnnualIntervalMonths,
    bool   IsActive,
    List<VaccineDoseStepDto> DoseSteps);

public record VaccinationRecordResponse(
    Guid     Id,
    Guid     PatientId,
    string   PatientName,
    Guid     VaccineDefinitionId,
    string   VaccineName,
    string   Scheme,
    int      DoseNumber,
    string   AdministeredAt,
    string   AdministeredByName,
    string?  BatchNumber,
    string?  NextDueDate,
    string   DueStatus,
    string?  Notes);

public record RegisterVaccinationRequest
{
    public Guid    VaccineDefinitionId  { get; set; }
    public string  AdministeredAt       { get; set; } = "";
    public string? BatchNumber          { get; set; }
    public string? Notes                { get; set; }
    public string? NextDueDateOverride  { get; set; }
}

public record CreateVaccineDefinitionRequest
{
    public string  Name                 { get; set; } = "";
    public string? Description          { get; set; }
    public string  Scheme               { get; set; } = "Annual";
    public int     AnnualIntervalMonths { get; set; } = 12;
    public List<VaccineDoseStepDto>? DoseSteps { get; set; }
}
