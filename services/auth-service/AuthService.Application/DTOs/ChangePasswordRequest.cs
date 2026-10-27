using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = ValidationMessages.PasswordRequired)]
    public string CurrentPassword { get; set; } = default!;

    [Required(ErrorMessage = ValidationMessages.PasswordRequired)]
    public string NewPassword { get; set; } = default!;
}
