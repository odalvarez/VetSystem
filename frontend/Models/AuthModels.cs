namespace VetSystem.Frontend.Models;

public class LoginRequest
{
    public string Email    { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    public string FirstName { get; set; } = "";
    public string LastName  { get; set; } = "";
    public string Email     { get; set; } = "";
    public string Password  { get; set; } = "";
    public string Phone     { get; set; } = "";
    public string Role      { get; set; } = "Owner";
}

// El token no llega al frontend — está en la cookie httpOnly del navegador.
// El backend solo devuelve { "user": { ... } }.
public class LoginResponse
{
    public LoginUserInfo User { get; set; } = new();
}

public class LoginUserInfo
{
    public Guid   Id       { get; set; }
    public string Email    { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role     { get; set; } = "";
    public string Phone    { get; set; } = "";
}

public class OwnerSummary
{
    public Guid   Id       { get; set; }
    public string FullName { get; set; } = "";
    public string Email    { get; set; } = "";
    public string Phone    { get; set; } = "";
}

public class UserProfile
{
    public Guid     Id        { get; set; }
    public string   FirstName { get; set; } = "";
    public string   LastName  { get; set; } = "";
    public string   Email     { get; set; } = "";
    public string   Phone     { get; set; } = "";
    public string   Role      { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
