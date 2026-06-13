using PatientsService.Domain.Exceptions;

namespace PatientsService.Domain.Entities;

public class ConsultationLog
{
    public Guid      Id                 { get; private set; }
    public Guid      PatientId          { get; private set; }
    public Guid?     AppointmentId      { get; private set; }
    public string    Status             { get; private set; } = "Open"; // "Open" | "Closed"
    public string    ReasonForVisit     { get; private set; } = default!;
    public string?   Anamnesis          { get; private set; }
    // Exploración física
    public string?   HeartRate          { get; private set; }
    public string?   RespiratoryRate    { get; private set; }
    public string?   BodyCondition      { get; private set; }
    public string?   MucousMembranes    { get; private set; }
    public string?   Hydration          { get; private set; }
    // Signos vitales
    public decimal?  WeightKg           { get; private set; }
    public decimal?  TemperatureCelsius { get; private set; }
    // Exámenes
    public string?   RequestedTests     { get; private set; }
    public string?   TestResults        { get; private set; }
    // Diagnóstico
    public string?   Diagnosis          { get; private set; }
    public string?   Prognosis          { get; private set; }
    // Planes
    public string?   TherapeuticPlan    { get; private set; }
    public string?   DiagnosticPlan     { get; private set; }
    // Cierre
    public string?   Recommendations    { get; private set; }
    public DateOnly? NextVisitDate      { get; private set; }
    // Metadatos
    public Guid      VeterinarianId     { get; private set; }
    public string    VeterinarianName   { get; private set; } = default!;
    public DateTime  OpenedAt           { get; private set; }
    public DateTime? ClosedAt           { get; private set; }

    public Patient   Patient            { get; private set; } = default!;

    private ConsultationLog() { }

    public static ConsultationLog Create(
        Guid patientId, Guid? appointmentId, string reasonForVisit,
        Guid veterinarianId, string veterinarianName,
        string? anamnesis,
        string? heartRate, string? respiratoryRate,
        string? bodyCondition, string? mucousMembranes, string? hydration,
        decimal? weightKg, decimal? temperatureCelsius,
        string? requestedTests, string? testResults,
        string? diagnosis, string? prognosis,
        string? therapeuticPlan, string? diagnosticPlan,
        string? recommendations, DateOnly? nextVisitDate)
    {
        if (string.IsNullOrWhiteSpace(reasonForVisit))
            throw new DomainException("El motivo de consulta es obligatorio.");

        return new ConsultationLog
        {
            Id                 = Guid.NewGuid(),
            PatientId          = patientId,
            AppointmentId      = appointmentId,
            Status             = "Open",
            ReasonForVisit     = reasonForVisit.Trim(),
            Anamnesis          = T(anamnesis),
            HeartRate          = T(heartRate),
            RespiratoryRate    = T(respiratoryRate),
            BodyCondition      = T(bodyCondition),
            MucousMembranes    = T(mucousMembranes),
            Hydration          = T(hydration),
            WeightKg           = weightKg,
            TemperatureCelsius = temperatureCelsius,
            RequestedTests     = T(requestedTests),
            TestResults        = T(testResults),
            Diagnosis          = T(diagnosis),
            Prognosis          = T(prognosis),
            TherapeuticPlan    = T(therapeuticPlan),
            DiagnosticPlan     = T(diagnosticPlan),
            Recommendations    = T(recommendations),
            NextVisitDate      = nextVisitDate,
            VeterinarianId     = veterinarianId,
            VeterinarianName   = veterinarianName,
            OpenedAt           = DateTime.UtcNow,
            ClosedAt           = null
        };
    }

    public void Update(
        string reasonForVisit,
        string? anamnesis,
        string? heartRate, string? respiratoryRate,
        string? bodyCondition, string? mucousMembranes, string? hydration,
        decimal? weightKg, decimal? temperatureCelsius,
        string? requestedTests, string? testResults,
        string? diagnosis, string? prognosis,
        string? therapeuticPlan, string? diagnosticPlan,
        string? recommendations, DateOnly? nextVisitDate)
    {
        if (Status == "Closed")
            throw new DomainException("No se puede editar una bitácora cerrada.");
        if (string.IsNullOrWhiteSpace(reasonForVisit))
            throw new DomainException("El motivo de consulta es obligatorio.");

        ReasonForVisit     = reasonForVisit.Trim();
        Anamnesis          = T(anamnesis);
        HeartRate          = T(heartRate);
        RespiratoryRate    = T(respiratoryRate);
        BodyCondition      = T(bodyCondition);
        MucousMembranes    = T(mucousMembranes);
        Hydration          = T(hydration);
        WeightKg           = weightKg;
        TemperatureCelsius = temperatureCelsius;
        RequestedTests     = T(requestedTests);
        TestResults        = T(testResults);
        Diagnosis          = T(diagnosis);
        Prognosis          = T(prognosis);
        TherapeuticPlan    = T(therapeuticPlan);
        DiagnosticPlan     = T(diagnosticPlan);
        Recommendations    = T(recommendations);
        NextVisitDate      = nextVisitDate;
    }

    public void Close()
    {
        if (Status == "Closed")
            throw new DomainException("La bitácora ya está cerrada.");
        Status   = "Closed";
        ClosedAt = DateTime.UtcNow;
    }

    private static string? T(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
