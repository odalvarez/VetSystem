namespace PatientsService.Application.DTOs;

// ── Catálogo de vacunas ───────────────────────────────────────────────────────

public record VaccineDoseStepDto(int DoseNumber, int DaysAfterPrevious);

public record VaccineDefinitionResponse(
    Guid   Id,
    string Name,
    string? Description,
    string Scheme,
    int    AnnualIntervalMonths,
    bool   IsActive,
    IReadOnlyList<VaccineDoseStepDto> DoseSteps);

public record CreateVaccineDefinitionRequest(
    string Name,
    string? Description,
    string Scheme,
    int    AnnualIntervalMonths,
    IReadOnlyList<VaccineDoseStepDto>? DoseSteps);

public record UpdateVaccineDefinitionRequest(
    string Name,
    string? Description,
    int    AnnualIntervalMonths,
    IReadOnlyList<VaccineDoseStepDto>? DoseSteps);

// ── Registros de vacunación ───────────────────────────────────────────────────

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

public record RegisterVaccinationRequest(
    Guid     VaccineDefinitionId,
    string   AdministeredAt,
    string?  BatchNumber,
    string?  Notes,
    string?  NextDueDateOverride);
