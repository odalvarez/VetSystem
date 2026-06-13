using PatientsService.Application.DTOs;
using PatientsService.Application.Exceptions;
using PatientsService.Application.Interfaces;
using PatientsService.Domain.Entities;
using PatientsService.Domain.Enums;

namespace PatientsService.Application.Services;

public class PatientAppService
{
    private readonly IPatientRepository _repo;
    private readonly ISpeciesRepository _speciesRepo;

    public PatientAppService(IPatientRepository repo, ISpeciesRepository speciesRepo)
    {
        _repo        = repo;
        _speciesRepo = speciesRepo;
    }

    public async Task<PatientResponse> CreateAsync(
        CreatePatientRequest req, Guid ownerId, string ownerName, string ownerPhone, CancellationToken ct)
    {
        var slug = req.Species.Trim().ToLowerInvariant();

        var species = await _speciesRepo.GetBySlugAsync(slug, ct)
            ?? throw new ValidationException($"Especie '{req.Species}' no válida.");

        if (!Enum.TryParse<Sex>(req.Sex, ignoreCase: true, out var sex))
            throw new ValidationException("Sexo no válido.");

        var patient = Patient.Create(
            req.Name, species.Id, req.Breed, req.BirthDate, sex, req.WeightKg,
            ownerId, ownerName, ownerPhone, req.Color, req.MicrochipNumber);

        await _repo.AddAsync(patient, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(patient, species.Slug, species.Name);
    }

    public async Task<PagedResponse<PatientResponse>> ListAsync(
        Guid? callerOwnerId, string? speciesSlug, string? search,
        int page, int pageSize, CancellationToken ct)
    {
        pageSize = Math.Min(pageSize, 100);

        Guid? speciesId = null;
        if (!string.IsNullOrWhiteSpace(speciesSlug))
        {
            var sp = await _speciesRepo.GetBySlugAsync(speciesSlug.ToLowerInvariant(), ct);
            speciesId = sp?.Id;
        }

        var (data, total) = await _repo.ListAsync(callerOwnerId, speciesId, search, page, pageSize, ct);

        // Carga todas las especies una sola vez para resolver slug y nombre sin N+1 queries
        var speciesList = await _speciesRepo.ListActiveAsync(ct);
        var speciesDict = speciesList.ToDictionary(s => s.Id);

        return new PagedResponse<PatientResponse>
        {
            Items = data.Select(p =>
            {
                speciesDict.TryGetValue(p.SpeciesId, out var sp2);
                return Map(p, sp2?.Slug ?? p.SpeciesId.ToString(), sp2?.Name ?? "Desconocida");
            }),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<PatientResponse> GetAsync(Guid id, Guid? callerOwnerId, CancellationToken ct)
    {
        var patient = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Mascota no encontrada.");

        if (callerOwnerId.HasValue && patient.OwnerId != callerOwnerId.Value)
            throw new ForbiddenException("No tiene permiso para ver esta mascota.");

        var species = await _speciesRepo.GetByIdAsync(patient.SpeciesId, ct);
        return Map(patient, species?.Slug ?? "", species?.Name ?? "Desconocida");
    }

    public async Task<PatientResponse> UpdateAsync(
        Guid id, UpdatePatientRequest req, Guid? callerOwnerId, CancellationToken ct)
    {
        var patient = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Mascota no encontrada.");

        if (callerOwnerId.HasValue && patient.OwnerId != callerOwnerId.Value)
            throw new ForbiddenException("No tiene permiso para modificar esta mascota.");

        if (!Enum.TryParse<Sex>(req.Sex, ignoreCase: true, out var sex))
            throw new ValidationException("Sexo no válido.");

        patient.Update(req.Name, req.Breed, req.BirthDate, sex, req.WeightKg, req.Color, req.MicrochipNumber);
        await _repo.UpdateAsync(patient, ct);
        await _repo.SaveChangesAsync(ct);

        var species = await _speciesRepo.GetByIdAsync(patient.SpeciesId, ct);
        return Map(patient, species?.Slug ?? "", species?.Name ?? "Desconocida");
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var patient = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Mascota no encontrada.");

        await _repo.DeleteAsync(patient, ct);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task DeleteByOwnerAsync(Guid ownerId, CancellationToken ct)
    {
        await _repo.DeleteByOwnerAsync(ownerId, ct);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task<ClinicalRecordResponse> AddRecordAsync(
        Guid patientId, CreateClinicalRecordRequest req,
        Guid vetId, string vetName, CancellationToken ct)
    {
        var patient = await _repo.GetByIdAsync(patientId, ct)
            ?? throw new NotFoundException("Mascota no encontrada.");

        var record = ClinicalRecord.Create(
            patient.Id, req.Date, req.Reason, req.Diagnosis,
            req.Treatment, req.Notes, vetId, vetName, req.NextVisitDate,
            req.WeightKg, req.TemperatureCelsius);

        await _repo.AddRecordAsync(record, ct);
        await _repo.SaveChangesAsync(ct);
        return MapRecord(record);
    }

    public async Task<PagedResponse<ClinicalRecordResponse>> ListRecordsAsync(
        Guid patientId, Guid? callerOwnerId, int page, int pageSize, CancellationToken ct)
    {
        pageSize = Math.Min(pageSize, 100);

        var patient = await _repo.GetByIdAsync(patientId, ct)
            ?? throw new NotFoundException("Mascota no encontrada.");

        if (callerOwnerId.HasValue && patient.OwnerId != callerOwnerId.Value)
            throw new ForbiddenException("No tiene permiso para ver esta historia clínica.");

        var (data, total) = await _repo.ListRecordsAsync(patientId, page, pageSize, ct);
        return new PagedResponse<ClinicalRecordResponse>
        {
            Items = data.Select(MapRecord), TotalCount = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<ClinicalRecordResponse> GetRecordAsync(
        Guid patientId, Guid recordId, Guid? callerOwnerId, CancellationToken ct)
    {
        var patient = await _repo.GetByIdAsync(patientId, ct)
            ?? throw new NotFoundException("Mascota no encontrada.");

        if (callerOwnerId.HasValue && patient.OwnerId != callerOwnerId.Value)
            throw new ForbiddenException("Sin permiso.");

        var record = await _repo.GetRecordAsync(patientId, recordId, ct)
            ?? throw new NotFoundException("Registro clínico no encontrado.");

        return MapRecord(record);
    }

    private static PatientResponse Map(Patient p, string speciesSlug, string speciesName) => new()
    {
        Id              = p.Id,
        Name            = p.Name,
        Species         = speciesSlug,
        SpeciesName     = speciesName,
        Breed           = p.Breed,
        BirthDate       = p.BirthDate,
        AgeYears        = CalcAge(p.BirthDate),
        Sex             = p.Sex.ToString().ToLowerInvariant(),
        WeightKg        = p.Weight,
        Color           = p.Color,
        MicrochipNumber = p.MicrochipNumber,
        OwnerId         = p.OwnerId,
        OwnerName       = p.OwnerName,
        OwnerPhone      = p.OwnerPhone,
        CreatedAt       = p.CreatedAt,
        UpdatedAt       = p.UpdatedAt
    };

    private static int CalcAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age   = today.Year - birthDate.Year;
        if (birthDate.AddYears(age) > today) age--;
        return Math.Max(0, age);
    }

    private static ClinicalRecordResponse MapRecord(ClinicalRecord r) => new()
    {
        Id                 = r.Id,
        PatientId          = r.PatientId,
        Date               = r.Date,
        Reason             = r.Reason,
        Diagnosis          = r.Diagnosis,
        Treatment          = r.Treatment,
        Notes              = r.Notes,
        WeightKg           = r.WeightKg,
        TemperatureCelsius = r.TemperatureCelsius,
        VeterinarianId     = r.VeterinarianId,
        VeterinarianName   = r.VeterinarianName,
        NextVisitDate      = r.NextVisitDate,
        CreatedAt          = r.CreatedAt
    };
}
