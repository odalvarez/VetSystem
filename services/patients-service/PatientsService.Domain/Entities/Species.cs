using PatientsService.Domain.Exceptions;

namespace PatientsService.Domain.Entities;

public class Species
{
    public Guid     Id        { get; private set; }
    /// Nombre para mostrar, ej. "Perro", "Tortuga"
    public string   Name      { get; private set; } = default!;
    /// Identificador técnico en minúsculas, ej. "dog", "tortuga"
    public string   Slug      { get; private set; } = default!;
    public bool      IsActive   { get; private set; }
    public DateTime  CreatedAt  { get; private set; }
    public bool      IsDeleted  { get; private set; }
    public DateTime? DeletedAt  { get; private set; }

    private Species() { }

    public static Species Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la especie es obligatorio.");

        return new Species
        {
            Id        = Guid.NewGuid(),
            Name      = name.Trim(),
            Slug      = GenerateSlug(name),
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive  = false;
        DeletedAt = DateTime.UtcNow;
    }

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la especie es obligatorio.");

        Name = name.Trim();
        // El slug no cambia para no romper referencias en Patients
    }

    // Slug = lowercase, espacios → guion bajo, sin tildes por simplicidad
    public static string GenerateSlug(string name) =>
        name.Trim().ToLowerInvariant()
            .Replace(' ', '_')
            .Replace('-', '_');
}
