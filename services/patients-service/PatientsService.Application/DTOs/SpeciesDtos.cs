using System.ComponentModel.DataAnnotations;

namespace PatientsService.Application.DTOs;

public class SpeciesResponse
{
    public Guid     Id           { get; set; }
    public string   Name         { get; set; } = default!;
    public string   Slug         { get; set; } = default!;
    public string   Icon         { get; set; } = "🐾";
    public bool     IsActive     { get; set; }
    public int      PatientCount { get; set; }
    public DateTime CreatedAt    { get; set; }
}

public class CreateSpeciesRequest
{
    [Required] [MaxLength(100)] public string Name { get; set; } = default!;
    [MaxLength(10)]              public string Icon { get; set; } = "🐾";
}

public class UpdateSpeciesRequest
{
    [Required] [MaxLength(100)] public string Name { get; set; } = default!;
    [MaxLength(10)]              public string? Icon { get; set; }
}
