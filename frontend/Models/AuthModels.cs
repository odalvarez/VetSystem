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

public class LoginResponse
{
    // El backend devuelve "accessToken"; Token es el alias que usamos internamente
    public string        AccessToken { get; set; } = "";
    public string        Token       => AccessToken;
    public int           ExpiresIn   { get; set; }
    public string        TokenType   { get; set; } = "Bearer";
    public LoginUserInfo User        { get; set; } = new();
}

public class LoginUserInfo
{
    public Guid   Id       { get; set; }
    public string Email    { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role     { get; set; } = "";
    // El teléfono no llega en el login response; se puede completar desde GetProfile si hace falta
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
