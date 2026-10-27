using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = ValidationMessages.EmailRequired)]
    [EmailAddress(ErrorMessage = ValidationMessages.EmailInvalid)]
    public string Email { get; set; } = default!;

    [Required(ErrorMessage = ValidationMessages.PasswordRequired)]
    public string Password { get; set; } = default!;
}
