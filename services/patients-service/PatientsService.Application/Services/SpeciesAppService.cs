using PatientsService.Application.DTOs;
using PatientsService.Application.Exceptions;
using PatientsService.Application.Interfaces;
using PatientsService.Domain.Entities;

namespace PatientsService.Application.Services;

public class SpeciesAppService
{
    private readonly ISpeciesRepository _repo;

    public SpeciesAppService(ISpeciesRepository repo) => _repo = repo;

    public async Task<List<SpeciesResponse>> ListAsync(CancellationToken ct)
    {
        var list   = await _repo.ListActiveAsync(ct);
        var counts = await _repo.GetPatientCountsBySlugAsync(ct);
        return list.Select(s => Map(s, counts.GetValueOrDefault(s.Slug, 0))).ToList();
    }

    public async Task<SpeciesResponse> CreateAsync(CreateSpeciesRequest req, CancellationToken ct)
    {
        var slug = Species.GenerateSlug(req.Name);

        var existing = await _repo.GetBySlugAsync(slug, ct);
        if (existing is not null)
            throw new ValidationException($"Ya existe una especie con el identificador '{slug}'.");

        var species = Species.Create(req.Name, slug, req.Icon);
        await _repo.AddAsync(species, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(species, 0);
    }

    public async Task<SpeciesResponse> UpdateAsync(Guid id, UpdateSpeciesRequest req, CancellationToken ct)
    {
        var species = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Especie no encontrada.");

        species.Update(req.Name, req.Icon);
        await _repo.SaveChangesAsync(ct);

        var count = await _repo.CountPatientsBySlugAsync(species.Slug, ct);
        return Map(species, count);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var species = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Especie no encontrada.");

        var count = await _repo.CountPatientsBySlugAsync(species.Slug, ct);
        if (count > 0)
            throw new ValidationException(
                $"No se puede eliminar '{species.Name}': hay {count} mascota(s) registrada(s) con esta especie.");

        await _repo.RemoveAsync(species, ct);
        await _repo.SaveChangesAsync(ct);
    }

    private static SpeciesResponse Map(Species s, int patientCount) => new()
    {
        Id           = s.Id,
        Name         = s.Name,
        Slug         = s.Slug,
        Icon         = s.Icon,
        IsActive     = s.IsActive,
        PatientCount = patientCount,
        CreatedAt    = s.CreatedAt
    };
}
