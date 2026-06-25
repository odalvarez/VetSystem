using PatientsService.Application.DTOs;
using PatientsService.Application.Exceptions;
using PatientsService.Application.Interfaces;
using PatientsService.Domain.Entities;

namespace PatientsService.Application.Services;

public class VaccinationAppService(
    IVaccinationRepository vaccinations,
    IPatientRepository     patients)
{
    // ── Catálogo de vacunas ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<VaccineDefinitionResponse>> ListDefinitionsAsync(CancellationToken ct)
    {
        var defs = await vaccinations.ListDefinitionsAsync(ct);
        return defs.Select(MapDefinition).ToList();
    }

    public async Task<VaccineDefinitionResponse> CreateDefinitionAsync(
        CreateVaccineDefinitionRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<VaccineScheme>(req.Scheme, true, out var scheme))
            throw new ValidationException("Esquema de vacuna inválido. Valores válidos: SingleDose, MultiDose, Annual.");

        var def = VaccineDefinition.Create(req.Name, scheme, req.Description, req.AnnualIntervalMonths);
        await vaccinations.AddDefinitionAsync(def, ct);
        await vaccinations.SaveChangesAsync(ct);

        if (scheme == VaccineScheme.MultiDose && req.DoseSteps?.Count > 0)
        {
            foreach (var step in req.DoseSteps.OrderBy(s => s.DoseNumber).Skip(1))
            {
                // La dosis 1 no tiene paso previo; los pasos definen la distancia entre dosis consecutivas
                var doseStep = VaccineDoseStep.Create(def.Id, step.DoseNumber, step.DaysAfterPrevious);
                await vaccinations.AddDoseStepAsync(doseStep, ct);
            }
            await vaccinations.SaveChangesAsync(ct);
        }

        var created = await vaccinations.GetDefinitionAsync(def.Id, ct);
        return MapDefinition(created!);
    }

    public async Task<VaccineDefinitionResponse> UpdateDefinitionAsync(
        Guid id, UpdateVaccineDefinitionRequest req, CancellationToken ct)
    {
        var def = await vaccinations.GetDefinitionAsync(id, ct)
            ?? throw new NotFoundException($"Vacuna {id} no encontrada.");

        def.Update(req.Name, req.Description, req.AnnualIntervalMonths);

        if (def.Scheme == VaccineScheme.MultiDose)
        {
            await vaccinations.RemoveDoseStepsAsync(id, ct);
            if (req.DoseSteps?.Count > 0)
            {
                foreach (var step in req.DoseSteps.OrderBy(s => s.DoseNumber).Skip(1))
                {
                    var doseStep = VaccineDoseStep.Create(def.Id, step.DoseNumber, step.DaysAfterPrevious);
                    await vaccinations.AddDoseStepAsync(doseStep, ct);
                }
            }
        }

        await vaccinations.SaveChangesAsync(ct);
        var updated = await vaccinations.GetDefinitionAsync(id, ct);
        return MapDefinition(updated!);
    }

    public async Task ToggleActiveAsync(Guid id, bool activate, CancellationToken ct)
    {
        var def = await vaccinations.GetDefinitionAsync(id, ct)
            ?? throw new NotFoundException($"Vacuna {id} no encontrada.");

        if (activate) def.Activate(); else def.Deactivate();
        await vaccinations.SaveChangesAsync(ct);
    }

    // ── Registros de vacunación ───────────────────────────────────────────────

    public async Task<IReadOnlyList<VaccinationRecordResponse>> ListByPatientAsync(
        Guid patientId, Guid callerId, string callerRole, CancellationToken ct)
    {
        var patient = await patients.GetByIdAsync(patientId, ct)
            ?? throw new NotFoundException($"Paciente {patientId} no encontrado.");

        if (callerRole == "Owner" && patient.OwnerId != callerId)
            throw new ForbiddenException("Solo puedes ver vacunas de tus propias mascotas.");

        var records = await vaccinations.ListByPatientAsync(patientId, ct);
        return records.Select(MapRecord).ToList();
    }

    public async Task<VaccinationRecordResponse> RegisterAsync(
        Guid patientId, RegisterVaccinationRequest req,
        Guid vetId, string vetName, CancellationToken ct)
    {
        var patient = await patients.GetByIdAsync(patientId, ct)
            ?? throw new NotFoundException($"Paciente {patientId} no encontrado.");

        var def = await vaccinations.GetDefinitionAsync(req.VaccineDefinitionId, ct)
            ?? throw new NotFoundException($"Vacuna {req.VaccineDefinitionId} no encontrada.");

        if (!DateOnly.TryParse(req.AdministeredAt, out var administeredAt))
            throw new ValidationException("Fecha de administración inválida.");

        var doseNumber = await vaccinations.GetNextDoseNumberAsync(patientId, def.Id, ct);

        // Calcula la fecha sugerida y permite que el vet la sobreescriba
        var suggested = def.CalculateNextDueDate(administeredAt, doseNumber);
        DateOnly? nextDueDate = suggested;
        if (!string.IsNullOrWhiteSpace(req.NextDueDateOverride))
        {
            if (!DateOnly.TryParse(req.NextDueDateOverride, out var overrideDate))
                throw new ValidationException("Fecha de próximo refuerzo inválida.");
            nextDueDate = overrideDate;
        }

        var record = VaccinationRecord.Create(
            patientId:          patientId,
            patientName:        patient.Name,
            ownerId:            patient.OwnerId,
            ownerName:          patient.OwnerName,
            ownerPhone:         patient.OwnerPhone,
            ownerEmail:         null,
            vaccineDefinitionId: def.Id,
            vaccineName:        def.Name,
            doseNumber:         doseNumber,
            administeredAt:     administeredAt,
            administeredById:   vetId,
            administeredByName: vetName,
            nextDueDate:        nextDueDate,
            batchNumber:        req.BatchNumber,
            notes:              req.Notes);

        await vaccinations.AddRecordAsync(record, ct);
        await vaccinations.SaveChangesAsync(ct);
        return MapRecord(record);
    }

    public async Task DeleteRecordAsync(Guid id, CancellationToken ct)
    {
        var record = await vaccinations.GetRecordAsync(id, ct)
            ?? throw new NotFoundException($"Registro de vacunación {id} no encontrado.");

        await vaccinations.DeleteRecordAsync(record.Id, ct);
        await vaccinations.SaveChangesAsync(ct);
    }

    // ── Mapeo ─────────────────────────────────────────────────────────────────

    private static VaccineDefinitionResponse MapDefinition(VaccineDefinition v) =>
        new(v.Id, v.Name, v.Description, v.Scheme.ToString(), v.AnnualIntervalMonths, v.IsActive,
            v.DoseSteps.OrderBy(s => s.DoseNumber)
                       .Select(s => new VaccineDoseStepDto(s.DoseNumber, s.DaysAfterPrevious))
                       .ToList());

    internal static VaccinationRecordResponse MapRecord(VaccinationRecord r)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var status = r.NextDueDate is null
            ? "none"
            : r.NextDueDate.Value < today
                ? "overdue"
                : r.NextDueDate.Value <= today.AddDays(7)
                    ? "upcoming"
                    : "ok";

        return new VaccinationRecordResponse(
            Id:                  r.Id,
            PatientId:           r.PatientId,
            PatientName:         r.PatientName,
            VaccineDefinitionId: r.VaccineDefinitionId,
            VaccineName:         r.VaccineName,
            Scheme:              "",
            DoseNumber:          r.DoseNumber,
            AdministeredAt:      r.AdministeredAt.ToString("yyyy-MM-dd"),
            AdministeredByName:  r.AdministeredByName,
            BatchNumber:         r.BatchNumber,
            NextDueDate:         r.NextDueDate?.ToString("yyyy-MM-dd"),
            DueStatus:           status,
            Notes:               r.Notes);
    }
}
