using System.ComponentModel.DataAnnotations;

namespace PatientsService.Application.DTOs;

public class CreatePatientRequest
{
    [Required] [MaxLength(100)] public string  Name            { get; set; } = default!;
    [Required]                  public string  Species         { get; set; } = default!;
    [Required] [MaxLength(100)] public string  Breed           { get; set; } = default!;
    [Required]                  public DateOnly BirthDate      { get; set; }
    [Required]                  public string  Sex             { get; set; } = default!;
    [Range(0.01, 999.99)]       public decimal WeightKg        { get; set; }
    [MaxLength(100)]            public string? Color           { get; set; }
    [MaxLength(50)]             public string? MicrochipNumber { get; set; }

    // Solo se usa cuando el que crea es veterinario; el owner lo toma del JWT
    public Guid?   OwnerId    { get; set; }
    public string? OwnerName  { get; set; }
    public string? OwnerPhone { get; set; }
}

public class UpdatePatientRequest
{
    [Required] [MaxLength(100)] public string  Name            { get; set; } = default!;
    [Required] [MaxLength(100)] public string  Breed           { get; set; } = default!;
    [Required]                  public DateOnly BirthDate      { get; set; }
    [Required]                  public string  Sex             { get; set; } = default!;
    [Range(0.01, 999.99)]       public decimal WeightKg        { get; set; }
    [MaxLength(100)]            public string? Color           { get; set; }
    [MaxLength(50)]             public string? MicrochipNumber { get; set; }
}

public class PatientResponse
{
    public Guid     Id              { get; set; }
    public string   Name            { get; set; } = default!;
    public string   Species         { get; set; } = default!;
    public string   SpeciesName     { get; set; } = default!;
    public string   SpeciesIcon     { get; set; } = default!;
    public string   Breed           { get; set; } = default!;
    public DateOnly BirthDate       { get; set; }
    public int      AgeYears        { get; set; }
    public string   Sex             { get; set; } = default!;
    public decimal  WeightKg        { get; set; }
    public string?  Color           { get; set; }
    public string?  MicrochipNumber { get; set; }
    public Guid     OwnerId         { get; set; }
    public string   OwnerName       { get; set; } = default!;
    public string   OwnerPhone      { get; set; } = default!;
    public DateTime CreatedAt       { get; set; }
    public DateTime UpdatedAt       { get; set; }
}

public class PagedResponse<T>
{
    public IEnumerable<T> Items     { get; set; } = default!;
    public int            TotalCount { get; set; }
    public int            Page      { get; set; }
    public int            PageSize  { get; set; }
}
