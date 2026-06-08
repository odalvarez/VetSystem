using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class UpdateProfileRequest
{
    [Required] [MaxLength(100)] public string FirstName { get; set; } = default!;
    [Required] [MaxLength(100)] public string LastName  { get; set; } = default!;
    [Required] [MaxLength(20)]  public string Phone     { get; set; } = default!;
}
