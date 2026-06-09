using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace VetSystem.Frontend.Services;

// Adapta el TokenProvider al sistema de autenticación de Blazor
// para que [Authorize] y AuthorizeView funcionen con nuestro JWT en memoria.
public class VetAuthStateProvider : AuthenticationStateProvider
{
    private readonly TokenProvider _tokenProvider;

    public VetAuthStateProvider(TokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
        // Cuando el token cambia, notificamos a Blazor para que re-evalúe rutas protegidas
        _tokenProvider.OnChange += NotifyChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_tokenProvider.IsAuthenticated || _tokenProvider.User is null)
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));

        var user = _tokenProvider.User;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Name,           user.FullName),
            // El rol viene en PascalCase del backend: Veterinarian | Owner
            new Claim(ClaimTypes.Role,           CapitalizeFirst(user.Role))
        };

        var identity  = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(new AuthenticationState(principal));
    }

    private void NotifyChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static string CapitalizeFirst(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..].ToLower();
}
