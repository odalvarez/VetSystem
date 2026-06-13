using PatientsService.Domain.Entities;
using PatientsService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PatientsService.API;

public static class SpeciesSeeder
{
    private static readonly (string Name, string Slug, string Icon)[] DefaultSpecies =
    [
        ("Perro",   "dog",    "🐶"),
        ("Gato",    "cat",    "🐱"),
        ("Ave",     "bird",   "🐦"),
        ("Conejo",  "rabbit", "🐰"),
        ("Otro",    "other",  "🐾"),
    ];

    public static async Task SeedAsync(PatientsDbContext db)
    {
        var existing = await db.Species
            .Where(s => !s.IsDeleted)
            .ToListAsync();

        var existingSlugs = existing.Select(s => s.Slug).ToHashSet();

        // Inserta las que faltan
        var toAdd = DefaultSpecies
            .Where(s => !existingSlugs.Contains(s.Slug))
            .Select(s => Species.Create(s.Name, s.Slug, s.Icon))
            .ToList();

        if (toAdd.Count > 0)
            await db.Species.AddRangeAsync(toAdd);

        // Corrige el ícono de las que ya existen pero tienen el valor vacío (migración antigua)
        foreach (var (_, slug, icon) in DefaultSpecies)
        {
            var sp = existing.FirstOrDefault(s => s.Slug == slug);
            if (sp is not null && sp.Icon == "")
                sp.Update(sp.Name, icon);
        }

        await db.SaveChangesAsync();
    }
}
