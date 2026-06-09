using VetSystem.Frontend.Models;

namespace VetSystem.Frontend.Services;

// El token vive solo en memoria: si el usuario cierra la pestaña, pierde la sesión.
// Decisión consciente para no usar localStorage y evitar XSS sobre el token.
public class TokenProvider
{
    private string?      _token;
    private LoginUserInfo? _user;

    public event Action? OnChange;

    public string? Token => _token;
    public LoginUserInfo? User => _user;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public void SetSession(string token, LoginUserInfo user)
    {
        _token = token;
        _user  = user;
        OnChange?.Invoke();
    }

    public void ClearSession()
    {
        _token = null;
        _user  = null;
        OnChange?.Invoke();
    }
}
