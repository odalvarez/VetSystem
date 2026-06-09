using VetSystem.Frontend.Models;

namespace VetSystem.Frontend.Services;

// El JWT vive en la cookie httpOnly del navegador; este proveedor solo guarda
// la info del usuario en memoria para que la UI sepa el nombre, rol, etc.
public class TokenProvider
{
    private LoginUserInfo? _user;
    private DateTime _lastActivity = DateTime.UtcNow;

    public event Action? OnChange;

    public LoginUserInfo? User            => _user;
    public bool           IsAuthenticated => _user is not null;

    public void RecordActivity() => _lastActivity = DateTime.UtcNow;

    // Devuelve true si el usuario lleva más de `timeout` sin interactuar
    public bool IsInactive(TimeSpan timeout) =>
        _user is not null && DateTime.UtcNow - _lastActivity > timeout;

    public void SetSession(LoginUserInfo user)
    {
        _user         = user;
        _lastActivity = DateTime.UtcNow;
        OnChange?.Invoke();
    }

    public void ClearSession()
    {
        _user = null;
        OnChange?.Invoke();
    }
}
