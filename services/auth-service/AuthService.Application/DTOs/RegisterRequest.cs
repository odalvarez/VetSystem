using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class RegisterRequest
{
    [Required] [MaxLength(100)] public string FirstName { get; set; } = default!;
    [Required] [MaxLength(100)] public string LastName  { get; set; } = default!;
    [Required] [EmailAddress]   public string Email     { get; set; } = default!;
    [Required] [MinLength(8)]   public string Password  { get; set; } = default!;
    [Required] [MaxLength(20)]  public string Phone     { get; set; } = default!;
    [Required]                  public string Role      { get; set; } = default!;
}
