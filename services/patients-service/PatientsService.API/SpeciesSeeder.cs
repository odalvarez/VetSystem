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
        // Si ya existe cualquier registro (activo o eliminado) la tabla fue inicializada antes — no tocar nada
        var anyExists = await db.Species.AnyAsync();
        if (anyExists)
            return;

        var toAdd = DefaultSpecies
            .Select(s => Species.Create(s.Name, s.Slug, s.Icon))
            .ToList();

        await db.Species.AddRangeAsync(toAdd);
        await db.SaveChangesAsync();
    }
}
