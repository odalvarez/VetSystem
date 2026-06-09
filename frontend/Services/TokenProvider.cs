using VetSystem.Frontend.Models;

namespace VetSystem.Frontend.Services;

// El JWT vive en la cookie httpOnly del navegador; este proveedor solo guarda
// la info del usuario en memoria para que la UI sepa el nombre, rol, etc.
public class TokenProvider
{
    private LoginUserInfo? _user;

    public event Action? OnChange;

    public LoginUserInfo? User            => _user;
    public bool           IsAuthenticated => _user is not null;

    public void SetSession(LoginUserInfo user)
    {
        _user = user;
        OnChange?.Invoke();
    }

    public void ClearSession()
    {
        _user = null;
        OnChange?.Invoke();
    }
}
