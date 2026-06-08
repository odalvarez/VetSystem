using System.ComponentModel.DataAnnotations;

namespace PatientsService.Application.DTOs;

public class CreatePatientRequest
{
    [Required] [MaxLength(100)] public string Name      { get; set; } = default!;
    [Required]                  public string Species   { get; set; } = default!;
    [Required] [MaxLength(100)] public string Breed     { get; set; } = default!;
    [Required]                  public DateOnly BirthDate { get; set; }
    [Required]                  public string Sex       { get; set; } = default!;
    [Range(0.01, 999.99)]       public decimal Weight   { get; set; }
}

public class UpdatePatientRequest
{
    [Required] [MaxLength(100)] public string Name      { get; set; } = default!;
    [Required] [MaxLength(100)] public string Breed     { get; set; } = default!;
    [Required]                  public DateOnly BirthDate { get; set; }
    [Required]                  public string Sex       { get; set; } = default!;
    [Range(0.01, 999.99)]       public decimal Weight   { get; set; }
}

public class PatientResponse
{
    public Guid      Id        { get; set; }
    public string    Name      { get; set; } = default!;
    public string    Species   { get; set; } = default!;
    public string    Breed     { get; set; } = default!;
    public DateOnly  BirthDate { get; set; }
    public string    Sex       { get; set; } = default!;
    public decimal   Weight    { get; set; }
    public Guid      OwnerId   { get; set; }
    public string    OwnerName { get; set; } = default!;
    public DateTime  CreatedAt { get; set; }
    public DateTime  UpdatedAt { get; set; }
}

public class PagedResponse<T>
{
    public IEnumerable<T> Data     { get; set; } = default!;
    public int            Total    { get; set; }
    public int            Page     { get; set; }
    public int            PageSize { get; set; }
}
