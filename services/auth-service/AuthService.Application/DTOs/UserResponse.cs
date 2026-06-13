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
    [Required] [MaxLength(50)]  public string FirstName { get; set; } = default!;
    [Required] [MaxLength(50)]  public string LastName  { get; set; } = default!;
    [Required] [EmailAddress]   public string Email     { get; set; } = default!;
    [Required] [MinLength(8)]   public string Password  { get; set; } = default!;
    [MaxLength(20)]             public string Phone     { get; set; } = "";
    public string Role { get; set; } = "Owner";
}

public class AdminUpdateUserRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName  { get; set; } = default!;
    public string Phone     { get; set; } = "";
    public string Role      { get; set; } = default!;
}

public class AdminResetPasswordRequest
{
    public string NewPassword { get; set; } = default!;
}
