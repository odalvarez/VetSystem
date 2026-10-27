using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class UserResponse
{
    public Guid     Id        { get; set; }
    public string   FirstName { get; set; } = default!;
    public string   LastName  { get; set; } = default!;
    public string   Email     { get; set; } = default!;
    public string   Phone     { get; set; } = default!;
    public string   Role      { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class RegisterResponse
{
    public Guid     Id        { get; set; }
    public string   Email     { get; set; } = default!;
    public string   Role      { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class LoginResponse
{
    public string AccessToken { get; set; } = default!;
    public int    ExpiresIn   { get; set; }
    public string TokenType   { get; set; } = "Bearer";
    public LoginUserInfo User { get; set; } = default!;
}

public class LoginUserInfo
{
    public Guid   Id       { get; set; }
    public string Email    { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Role     { get; set; } = default!;
    public string Phone    { get; set; } = "";
}

public class OwnerSummary
{
    public Guid   Id       { get; set; }
    public string FullName { get; set; } = default!;
    public string Email    { get; set; } = default!;
    public string Phone    { get; set; } = default!;
}

public class AdminUserItem
{
    public Guid     Id        { get; set; }
    public string   FirstName { get; set; } = default!;
    public string   LastName  { get; set; } = default!;
    public string   Email     { get; set; } = default!;
    public string   Phone     { get; set; } = default!;
    public string   Role      { get; set; } = default!;
    public bool     IsActive  { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminPagedUsers
{
    public IEnumerable<AdminUserItem> Items      { get; set; } = [];
    public int                        TotalCount { get; set; }
    public int                        Page       { get; set; }
    public int                        PageSize   { get; set; }
}

public class AdminCreateUserRequest
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

public class AdminUpdateUserRequest
{
    [Required(ErrorMessage = ValidationMessages.FirstNameRequired)]
    [MaxLength(ValidationMessages.NameMaxLength, ErrorMessage = ValidationMessages.FirstNameTooLong)]
    public string FirstName { get; set; } = default!;

    [Required(ErrorMessage = ValidationMessages.LastNameRequired)]
    [MaxLength(ValidationMessages.NameMaxLength, ErrorMessage = ValidationMessages.LastNameTooLong)]
    public string LastName { get; set; } = default!;

    [MaxLength(ValidationMessages.PhoneMaxLength, ErrorMessage = ValidationMessages.PhoneTooLong)]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = ValidationMessages.RoleRequired)]
    public string Role { get; set; } = default!;
}

public class AdminResetPasswordRequest
{
    [Required(ErrorMessage = ValidationMessages.PasswordRequired)]
    public string NewPassword { get; set; } = default!;
}
