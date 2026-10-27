using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = ValidationMessages.FirstNameRequired)]
    [MaxLength(ValidationMessages.NameMaxLength, ErrorMessage = ValidationMessages.FirstNameTooLong)]
    public string FirstName { get; set; } = default!;

    [Required(ErrorMessage = ValidationMessages.LastNameRequired)]
    [MaxLength(ValidationMessages.NameMaxLength, ErrorMessage = ValidationMessages.LastNameTooLong)]
    public string LastName { get; set; } = default!;

    [Required(ErrorMessage = ValidationMessages.EmailRequired)]
    [EmailAddress(ErrorMessage = ValidationMessages.EmailInvalid)]
    [MaxLength(ValidationMessages.EmailMaxLength, ErrorMessage = ValidationMessages.EmailTooLong)]
    public string Email { get; set; } = default!;

    [Required(ErrorMessage = ValidationMessages.PasswordRequired)]
    public string Password { get; set; } = default!;

    [MaxLength(ValidationMessages.PhoneMaxLength, ErrorMessage = ValidationMessages.PhoneTooLong)]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = ValidationMessages.RoleRequired)]
    public string Role { get; set; } = default!;
}
