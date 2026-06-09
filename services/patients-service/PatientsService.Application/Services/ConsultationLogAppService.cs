using PatientsService.Application.DTOs;
using PatientsService.Application.Exceptions;
using PatientsService.Application.Interfaces;
using PatientsService.Domain.Entities;

namespace PatientsService.Application.Services;

public class ConsultationLogAppService
{
    private readonly IConsultationLogRepository _repo;
    private readonly IPatientRepository         _patients;

    public ConsultationLogAppService(IConsultationLogRepository repo, IPatientRepository patients)
    {
        _repo     = repo;
        _patients = patients;
    }

    public async Task<ConsultationLogResponse> CreateAsync(
        Guid patientId, CreateConsultationLogRequest req,
        Guid vetId, string vetName, CancellationToken ct)
    {
        // Verifica que la mascota exista
        var patient = await _patients.GetByIdAsync(patientId, ct)
            ?? throw new NotFoundException("Mascota no encontrada.");

        var log = ConsultationLog.Create(
            patient.Id, req.ReasonForVisit, vetId, vetName,
            req.Anamnesis,
            req.HeartRate, req.RespiratoryRate,
            req.BodyCondition, req.MucousMembranes, req.Hydration,
            req.WeightKg, req.TemperatureCelsius,
            req.RequestedTests, req.TestResults,
            req.Diagnosis, req.Prognosis,
            req.TherapeuticPlan, req.DiagnosticPlan,
            req.Recommendations, req.NextVisitDate);

        await _repo.AddAsync(log, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(log);
    }

    public async Task<PagedResponse<ConsultationLogResponse>> ListAsync(
        Guid patientId, Guid? callerOwnerId, int page, int pageSize, CancellationToken ct)
    {
        var patient = await _patients.GetByIdAsync(patientId, ct)
            ?? throw new NotFoundException("Mascota no encontrada.");

        if (callerOwnerId.HasValue && patient.OwnerId != callerOwnerId.Value)
            throw new ForbiddenException("No tiene permiso para ver las bitácoras de esta mascota.");

        pageSize = Math.Min(pageSize, 100);
        var (data, total) = await _repo.ListByPatientAsync(patientId, page, pageSize, ct);
        return new PagedResponse<ConsultationLogResponse>
        {
            Items      = data.Select(Map),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<ConsultationLogResponse> GetAsync(
        Guid patientId, Guid logId, Guid? callerOwnerId, CancellationToken ct)
    {
        var patient = await _patients.GetByIdAsync(patientId, ct)
            ?? throw new NotFoundException("Mascota no encontrada.");

        if (callerOwnerId.HasValue && patient.OwnerId != callerOwnerId.Value)
            throw new ForbiddenException("Sin permiso.");

        var log = await _repo.GetByIdAsync(logId, ct)
            ?? throw new NotFoundException("Bitácora no encontrada.");

        if (log.PatientId != patientId)
            throw new NotFoundException("Bitácora no encontrada.");

        return Map(log);
    }

    public async Task<ConsultationLogResponse> UpdateAsync(
        Guid patientId, Guid logId, UpdateConsultationLogRequest req,
        CancellationToken ct)
    {
        _ = await _patients.GetByIdAsync(patientId, ct)
            ?? throw new NotFoundException("Mascota no encontrada.");

        var log = await _repo.GetByIdAsync(logId, ct)
            ?? throw new NotFoundException("Bitácora no encontrada.");

        if (log.PatientId != patientId)
            throw new NotFoundException("Bitácora no encontrada.");

        log.Update(
            req.ReasonForVisit, req.Anamnesis,
            req.HeartRate, req.RespiratoryRate,
            req.BodyCondition, req.MucousMembranes, req.Hydration,
            req.WeightKg, req.TemperatureCelsius,
            req.RequestedTests, req.TestResults,
            req.Diagnosis, req.Prognosis,
            req.TherapeuticPlan, req.DiagnosticPlan,
            req.Recommendations, req.NextVisitDate);

        await _repo.UpdateAsync(log, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(log);
    }

    public async Task<ConsultationLogResponse> CloseAsync(
        Guid patientId, Guid logId, CancellationToken ct)
    {
        _ = await _patients.GetByIdAsync(patientId, ct)
            ?? throw new NotFoundException("Mascota no encontrada.");

        var log = await _repo.GetByIdAsync(logId, ct)
            ?? throw new NotFoundException("Bitácora no encontrada.");

        if (log.PatientId != patientId)
            throw new NotFoundException("Bitácora no encontrada.");

        log.Close();
        await _repo.UpdateAsync(log, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(log);
    }

    private static ConsultationLogResponse Map(ConsultationLog l) => new()
    {
        Id                 = l.Id,
        PatientId          = l.PatientId,
        Status             = l.Status,
        ReasonForVisit     = l.ReasonForVisit,
        Anamnesis          = l.Anamnesis,
        HeartRate          = l.HeartRate,
        RespiratoryRate    = l.RespiratoryRate,
        BodyCondition      = l.BodyCondition,
        MucousMembranes    = l.MucousMembranes,
        Hydration          = l.Hydration,
        WeightKg           = l.WeightKg,
        TemperatureCelsius = l.TemperatureCelsius,
        RequestedTests     = l.RequestedTests,
        TestResults        = l.TestResults,
        Diagnosis          = l.Diagnosis,
        Prognosis          = l.Prognosis,
        TherapeuticPlan    = l.TherapeuticPlan,
        DiagnosticPlan     = l.DiagnosticPlan,
        Recommendations    = l.Recommendations,
        NextVisitDate      = l.NextVisitDate,
        VeterinarianId     = l.VeterinarianId,
        VeterinarianName   = l.VeterinarianName,
        OpenedAt           = l.OpenedAt,
        ClosedAt           = l.ClosedAt
    };
}
