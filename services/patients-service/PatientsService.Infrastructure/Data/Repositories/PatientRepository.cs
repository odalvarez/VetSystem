using Microsoft.EntityFrameworkCore;
using PatientsService.Application.Interfaces;
using PatientsService.Domain.Entities;

namespace PatientsService.Infrastructure.Data.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly PatientsDbContext _db;

    public PatientRepository(PatientsDbContext db) => _db = db;

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Patients
           .Include(p => p.ClinicalRecords)
           .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IEnumerable<Patient> Data, int Total)> ListAsync(
        Guid? ownerId, string? species, string? search,
        int page, int pageSize, CancellationToken ct)
    {
        var q = _db.Patients.AsQueryable();

        if (ownerId.HasValue)
            q = q.Where(p => p.OwnerId == ownerId.Value);

        if (!string.IsNullOrWhiteSpace(species))
            q = q.Where(p => p.Species.ToString().ToLower() == species.ToLower());

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
        _db.Patients.Remove(patient);
        return Task.CompletedTask;
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
