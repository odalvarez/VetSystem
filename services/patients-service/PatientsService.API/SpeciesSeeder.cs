using PatientsService.Domain.Entities;
using PatientsService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PatientsService.API;

public static class SpeciesSeeder
{
    private static readonly (string Name, string Slug)[] DefaultSpecies =
    [
        ("Perro",   "dog"),
        ("Gato",    "cat"),
        ("Ave",     "bird"),
        ("Conejo",  "rabbit"),
        ("Otro",    "other"),
    ];

    public static async Task SeedAsync(PatientsDbContext db)
    {
        var existingSlugs = await db.Species
            .Where(s => !s.IsDeleted)
            .Select(s => s.Slug)
            .ToHashSetAsync();

        var toAdd = DefaultSpecies
            .Where(s => !existingSlugs.Contains(s.Slug))
            .Select(s => Species.Create(s.Name, s.Slug))
            .ToList();

        if (toAdd.Count == 0) return;

        await db.Species.AddRangeAsync(toAdd);
        await db.SaveChangesAsync();
    }
}
