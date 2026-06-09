using System.ComponentModel.DataAnnotations;

namespace PatientsService.Application.DTOs;

public class CreateClinicalRecordRequest
{
    [Required]                  public DateTime  Date               { get; set; } = DateTime.UtcNow;
    [Required][MaxLength(500)]  public string    Reason             { get; set; } = default!;
    [Required][MaxLength(500)]  public string    Diagnosis          { get; set; } = default!;
    [Required][MaxLength(500)]  public string    Treatment          { get; set; } = default!;
    [MaxLength(1000)]           public string?   Notes              { get; set; }
    public DateOnly?  NextVisitDate      { get; set; }
    public decimal?   WeightKg           { get; set; }
    public decimal?   TemperatureCelsius { get; set; }
}

public class ClinicalRecordResponse
{
    public Guid      Id                 { get; set; }
    public Guid      PatientId          { get; set; }
    public DateTime  Date               { get; set; }
    public string    Reason             { get; set; } = default!;
    public string    Diagnosis          { get; set; } = default!;
    public string    Treatment          { get; set; } = default!;
    public string?   Notes              { get; set; }
    public decimal?  WeightKg           { get; set; }
    public decimal?  TemperatureCelsius { get; set; }
    public Guid      VeterinarianId     { get; set; }
    public string    VeterinarianName   { get; set; } = default!;
    public DateOnly? NextVisitDate      { get; set; }
    public DateTime  CreatedAt          { get; set; }
}
