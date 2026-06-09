using System.ComponentModel.DataAnnotations;

namespace PatientsService.Application.DTOs;

public class CreateConsultationLogRequest
{
    [Required][MaxLength(500)]  public string    ReasonForVisit     { get; set; } = default!;
    [MaxLength(2000)]           public string?   Anamnesis          { get; set; }
    // Exploración física
    [MaxLength(100)]            public string?   HeartRate          { get; set; }
    [MaxLength(100)]            public string?   RespiratoryRate    { get; set; }
    [MaxLength(200)]            public string?   BodyCondition      { get; set; }
    [MaxLength(200)]            public string?   MucousMembranes    { get; set; }
    [MaxLength(200)]            public string?   Hydration          { get; set; }
    // Vitales
                                public decimal?  WeightKg           { get; set; }
                                public decimal?  TemperatureCelsius { get; set; }
    // Exámenes
    [MaxLength(2000)]           public string?   RequestedTests     { get; set; }
    [MaxLength(2000)]           public string?   TestResults        { get; set; }
    // Diagnóstico
    [MaxLength(1000)]           public string?   Diagnosis          { get; set; }
    [MaxLength(1000)]           public string?   Prognosis          { get; set; }
    // Planes
    [MaxLength(2000)]           public string?   TherapeuticPlan    { get; set; }
    [MaxLength(2000)]           public string?   DiagnosticPlan     { get; set; }
    // Cierre
    [MaxLength(1000)]           public string?   Recommendations    { get; set; }
                                public DateOnly? NextVisitDate      { get; set; }
}

// Update reutiliza exactamente los mismos campos que Create
public class UpdateConsultationLogRequest : CreateConsultationLogRequest { }

public class ConsultationLogResponse
{
    public Guid      Id                 { get; set; }
    public Guid      PatientId          { get; set; }
    public string    Status             { get; set; } = "";
    public string    ReasonForVisit     { get; set; } = "";
    public string?   Anamnesis          { get; set; }
    public string?   HeartRate          { get; set; }
    public string?   RespiratoryRate    { get; set; }
    public string?   BodyCondition      { get; set; }
    public string?   MucousMembranes    { get; set; }
    public string?   Hydration          { get; set; }
    public decimal?  WeightKg           { get; set; }
    public decimal?  TemperatureCelsius { get; set; }
    public string?   RequestedTests     { get; set; }
    public string?   TestResults        { get; set; }
    public string?   Diagnosis          { get; set; }
    public string?   Prognosis          { get; set; }
    public string?   TherapeuticPlan    { get; set; }
    public string?   DiagnosticPlan     { get; set; }
    public string?   Recommendations    { get; set; }
    public DateOnly? NextVisitDate      { get; set; }
    public Guid      VeterinarianId     { get; set; }
    public string    VeterinarianName   { get; set; } = "";
    public DateTime  OpenedAt           { get; set; }
    public DateTime? ClosedAt           { get; set; }
}
