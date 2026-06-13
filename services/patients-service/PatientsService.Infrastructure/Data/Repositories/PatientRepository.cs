using Microsoft.EntityFrameworkCore;
using PatientsService.Application.Interfaces;
using PatientsService.Domain.Entities;

namespace PatientsService.Infrastructure.Data.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly PatientsDbContext _db;

    public PatientRepository(PatientsDbContext db) => _db = db;

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Patients.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

    public async Task<(IEnumerable<Patient> Data, int Total)> ListAsync(
        Guid? ownerId, Guid? speciesId, string? search,
        int page, int pageSize, CancellationToken ct)
    {
        var q = _db.Patients.Where(p => !p.IsDeleted);

        if (ownerId.HasValue)
            q = q.Where(p => p.OwnerId == ownerId.Value);

        if (speciesId.HasValue)
            q = q.Where(p => p.SpeciesId == speciesId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search) || p.OwnerName.Contains(search));

        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(p => p.Name)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync(ct);
        return (data, total);
    }

    public async Task AddAsync(Patient patient, CancellationToken ct) =>
        await _db.Patients.AddAsync(patient, ct);

    public Task UpdateAsync(Patient patient, CancellationToken ct)
    {
        _db.Patients.Update(patient);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Patient patient, CancellationToken ct)
    {
        patient.SoftDelete();
        _db.Patients.Update(patient);
        return Task.CompletedTask;
    }

    public async Task DeleteByOwnerAsync(Guid ownerId, CancellationToken ct)
    {
        var patients = await _db.Patients
            .Where(p => p.OwnerId == ownerId && !p.IsDeleted)
            .ToListAsync(ct);

        foreach (var p in patients)
            p.SoftDelete();

        _db.Patients.UpdateRange(patients);
    }

    public Task<ClinicalRecord?> GetRecordAsync(Guid patientId, Guid recordId, CancellationToken ct) =>
        _db.ClinicalRecords
           .FirstOrDefaultAsync(r => r.PatientId == patientId && r.Id == recordId, ct);

    public async Task<(IEnumerable<ClinicalRecord> Data, int Total)> ListRecordsAsync(
        Guid patientId, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.ClinicalRecords.Where(r => r.PatientId == patientId);
        var total = await q.CountAsync(ct);
        var data  = await q.OrderByDescending(r => r.Date)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToListAsync(ct);
        return (data, total);
    }

    public async Task AddRecordAsync(ClinicalRecord record, CancellationToken ct) =>
        await _db.ClinicalRecords.AddAsync(record, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        _db.SaveChangesAsync(ct);
}
