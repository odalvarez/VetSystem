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
           .Where(s => s.IsActive)
           .OrderBy(s => s.Name)
           .ToListAsync(ct);

    public Task<Species?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Species.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Species?> GetBySlugAsync(string slug, CancellationToken ct) =>
        _db.Species.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive, ct);

    public Task<int> CountPatientsBySlugAsync(string slug, CancellationToken ct) =>
        _db.Patients.CountAsync(p => p.Species == slug, ct);

    public Task<Dictionary<string, int>> GetPatientCountsBySlugAsync(CancellationToken ct) =>
        _db.Patients
           .GroupBy(p => p.Species)
           .Select(g => new { g.Key, Count = g.Count() })
           .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

    public async Task AddAsync(Species species, CancellationToken ct) =>
        await _db.Species.AddAsync(species, ct);

    public Task RemoveAsync(Species species, CancellationToken ct)
    {
        _db.Species.Remove(species);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) =>
        _db.SaveChangesAsync(ct);
}
