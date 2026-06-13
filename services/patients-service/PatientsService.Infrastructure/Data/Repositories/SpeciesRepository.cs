using Microsoft.EntityFrameworkCore;
using PatientsService.Application.Interfaces;
using PatientsService.Domain.Entities;

namespace PatientsService.Infrastructure.Data.Repositories;

public class SpeciesRepository : ISpeciesRepository
{
    private readonly PatientsDbContext _db;

    public SpeciesRepository(PatientsDbContext db) => _db = db;

    public Task<List<Species>> ListActiveAsync(CancellationToken ct) =>
        _db.Species
           .Where(s => s.IsActive && !s.IsDeleted)
           .OrderBy(s => s.Name)
           .ToListAsync(ct);

    public Task<Species?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Species.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);

    public Task<Species?> GetBySlugAsync(string slug, CancellationToken ct) =>
        _db.Species.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive && !s.IsDeleted, ct);

    public Task<int> CountPatientsByIdAsync(Guid speciesId, CancellationToken ct) =>
        _db.Patients.CountAsync(p => p.SpeciesId == speciesId && !p.IsDeleted, ct);

    public Task<Dictionary<Guid, int>> GetPatientCountsByIdAsync(CancellationToken ct) =>
        _db.Patients
           .Where(p => !p.IsDeleted)
           .GroupBy(p => p.SpeciesId)
           .Select(g => new { g.Key, Count = g.Count() })
           .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

    public async Task AddAsync(Species species, CancellationToken ct) =>
        await _db.Species.AddAsync(species, ct);

    public Task RemoveAsync(Species species, CancellationToken ct)
    {
        species.SoftDelete();
        _db.Species.Update(species);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) =>
        _db.SaveChangesAsync(ct);
}
