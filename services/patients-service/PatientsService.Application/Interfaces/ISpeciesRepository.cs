using PatientsService.Domain.Entities;

namespace PatientsService.Application.Interfaces;

public interface ISpeciesRepository
{
    Task<List<Species>>           ListActiveAsync(CancellationToken ct = default);
    Task<Species?>                GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Species?>                GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<int>                     CountPatientsBySlugAsync(string slug, CancellationToken ct = default);
    Task<Dictionary<string, int>> GetPatientCountsBySlugAsync(CancellationToken ct = default);
    Task                AddAsync(Species species, CancellationToken ct = default);
    Task                RemoveAsync(Species species, CancellationToken ct = default);
    Task                SaveChangesAsync(CancellationToken ct = default);
}
