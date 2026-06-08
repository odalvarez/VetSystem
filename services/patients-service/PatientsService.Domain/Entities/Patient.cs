using PatientsService.Domain.Enums;
using PatientsService.Domain.Exceptions;

namespace PatientsService.Domain.Entities;

public class Patient
{
    public Guid      Id        { get; private set; }
    public string    Name      { get; private set; } = default!;
    public Species   Species   { get; private set; }
    public string    Breed     { get; private set; } = default!;
    public DateOnly  BirthDate { get; private set; }
    public Sex       Sex       { get; private set; }
    public decimal   Weight    { get; private set; }
    public Guid      OwnerId   { get; private set; }
    public string    OwnerName { get; private set; } = default!;
    public DateTime  CreatedAt { get; private set; }
    public DateTime  UpdatedAt { get; private set; }

    public ICollection<ClinicalRecord> ClinicalRecords { get; private set; } = new List<ClinicalRecord>();

    private Patient() { }

    public static Patient Create(
        string name, Species species, string breed,
        DateOnly birthDate, Sex sex, decimal weight,
        Guid ownerId, string ownerName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la mascota es obligatorio.");

        if (weight <= 0)
            throw new DomainException("El peso debe ser mayor a cero.");

        return new Patient
        {
            Id        = Guid.NewGuid(),
            Name      = name.Trim(),
            Species   = species,
            Breed     = breed.Trim(),
            BirthDate = birthDate,
            Sex       = sex,
            Weight    = weight,
            OwnerId   = ownerId,
            OwnerName = ownerName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string breed, DateOnly birthDate, Sex sex, decimal weight)
    {
        if (weight <= 0)
            throw new DomainException("El peso debe ser mayor a cero.");

        Name      = name.Trim();
        Breed     = breed.Trim();
        BirthDate = birthDate;
        Sex       = sex;
        Weight    = weight;
        UpdatedAt = DateTime.UtcNow;
    }
}
