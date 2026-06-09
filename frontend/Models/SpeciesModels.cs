namespace VetSystem.Frontend.Models;

public class SpeciesResponse
{
    public Guid     Id           { get; set; }
    public string   Name         { get; set; } = "";
    public string   Slug         { get; set; } = "";
    public bool     IsActive     { get; set; }
    public int      PatientCount { get; set; }
    public DateTime CreatedAt    { get; set; }
}

public class CreateSpeciesRequest
{
    public string Name { get; set; } = "";
}

public class UpdateSpeciesRequest
{
    public string Name { get; set; } = "";
}
