using PatientsService.Domain.Enums;
using PatientsService.Domain.Exceptions;

namespace PatientsService.Domain.Entities;

public class Patient
{
    public Guid      Id              { get; private set; }
    public string    Name            { get; private set; } = default!;
    public Guid      SpeciesId       { get; private set; }
    public string    Breed           { get; private set; } = default!;
    public DateOnly  BirthDate       { get; private set; }
    public Sex       Sex             { get; private set; }
    public decimal   Weight          { get; private set; }
    public string?   Color           { get; private set; }
    public string?   MicrochipNumber { get; private set; }
    public Guid      OwnerId         { get; private set; }
    public string    OwnerName       { get; private set; } = default!;
    public string    OwnerPhone      { get; private set; } = "";
    public DateTime  CreatedAt       { get; private set; }
    public DateTime  UpdatedAt       { get; private set; }
    public bool      IsDeleted       { get; private set; }
    public DateTime? DeletedAt       { get; private set; }

    public ICollection<ClinicalRecord>   ClinicalRecords   { get; private set; } = new List<ClinicalRecord>();
    public ICollection<ConsultationLog>  ConsultationLogs  { get; private set; } = new List<ConsultationLog>();

    private Patient() { }

    public static Patient Create(
        string name, Guid speciesId, string breed,
        DateOnly birthDate, Sex sex, decimal weight,
        Guid ownerId, string ownerName, string ownerPhone = "",
        string? color = null, string? microchipNumber = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la mascota es obligatorio.");

        if (weight <= 0)
            throw new DomainException("El peso debe ser mayor a cero.");

        return new Patient
        {
            Id              = Guid.NewGuid(),
            Name            = name.Trim(),
            SpeciesId       = speciesId,
            Breed           = breed.Trim(),
            BirthDate       = birthDate,
            Sex             = sex,
            Weight          = weight,
            Color           = color?.Trim(),
            MicrochipNumber = microchipNumber?.Trim(),
            OwnerId         = ownerId,
            OwnerName       = ownerName,
            OwnerPhone      = ownerPhone,
            CreatedAt       = DateTime.UtcNow,
            UpdatedAt       = DateTime.UtcNow
        };
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string breed, DateOnly birthDate, Sex sex, decimal weight,
                       string? color = null, string? microchipNumber = null)
    {
        if (weight <= 0)
            throw new DomainException("El peso debe ser mayor a cero.");

        Name            = name.Trim();
        Breed           = breed.Trim();
        BirthDate       = birthDate;
        Sex             = sex;
        Weight          = weight;
        Color           = color?.Trim();
        MicrochipNumber = microchipNumber?.Trim();
        UpdatedAt       = DateTime.UtcNow;
    }
}
