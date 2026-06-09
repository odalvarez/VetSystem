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
