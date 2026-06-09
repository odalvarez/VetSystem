namespace VetSystem.Frontend.Models;

public class ConsultationLogResponse
{
    public Guid      Id                 { get; set; }
    public Guid      PatientId          { get; set; }
    public string    Status             { get; set; } = "";   // "Open" | "Closed"
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

    public bool IsOpen   => Status == "Open";
    public bool IsClosed => Status == "Closed";
}

public class CreateConsultationLogRequest
{
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
}
