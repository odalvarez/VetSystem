namespace VetSystem.Frontend.Models;

public class CreatePatientRequest
{
    public string   Name            { get; set; } = "";
    public string   Species         { get; set; } = "";
    public string   Breed           { get; set; } = "";
    public string   Color           { get; set; } = "";
    public string   Sex             { get; set; } = "";
    public int      AgeYears        { get; set; }
    public decimal  WeightKg        { get; set; }
    public string?  MicrochipNumber { get; set; }
}

public class PatientResponse
{
    public Guid     Id              { get; set; }
    public string   Name            { get; set; } = "";
    public string   Species         { get; set; } = "";
    public string   Breed           { get; set; } = "";
    public string   Color           { get; set; } = "";
    public string   Sex             { get; set; } = "";
    public int      AgeYears        { get; set; }
    public decimal  WeightKg        { get; set; }
    public string?  MicrochipNumber { get; set; }
    public Guid     OwnerId         { get; set; }
    public string   OwnerName       { get; set; } = "";
    public string   OwnerPhone      { get; set; } = "";
    public DateTime CreatedAt       { get; set; }
    public DateTime UpdatedAt       { get; set; }
}

public class CreateClinicalRecordRequest
{
    public string   Diagnosis          { get; set; } = "";
    public string   Treatment          { get; set; } = "";
    public string?  Notes              { get; set; }
    public decimal? WeightKg           { get; set; }
    public decimal? TemperatureCelsius { get; set; }
}

public class ClinicalRecordResponse
{
    public Guid     Id                 { get; set; }
    public Guid     PatientId          { get; set; }
    public string   Diagnosis          { get; set; } = "";
    public string   Treatment          { get; set; } = "";
    public string?  Notes              { get; set; }
    public decimal? WeightKg           { get; set; }
    public decimal? TemperatureCelsius { get; set; }
    public Guid     VeterinarianId     { get; set; }
    public string   VeterinarianName   { get; set; } = "";
    public DateTime CreatedAt          { get; set; }
}

// Items / TotalCount es la convención que usamos en todos los componentes
public class PagedResponse<T>
{
    public List<T> Items      { get; set; } = new();
    public int     TotalCount { get; set; }
    public int     Page       { get; set; }
    public int     PageSize   { get; set; }
}
