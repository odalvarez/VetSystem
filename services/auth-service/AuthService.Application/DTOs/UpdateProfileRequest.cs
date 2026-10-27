using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = ValidationMessages.FirstNameRequired)]
    [MaxLength(ValidationMessages.NameMaxLength, ErrorMessage = ValidationMessages.FirstNameTooLong)]
    public string FirstName { get; set; } = default!;

    [Required(ErrorMessage = ValidationMessages.LastNameRequired)]
    [MaxLength(ValidationMessages.NameMaxLength, ErrorMessage = ValidationMessages.LastNameTooLong)]
    public string LastName { get; set; } = default!;

    [MaxLength(ValidationMessages.PhoneMaxLength, ErrorMessage = ValidationMessages.PhoneTooLong)]
    public string Phone { get; set; } = "";
}
