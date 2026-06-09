using System.Net;
using Microsoft.AspNetCore.Components;

namespace VetSystem.Frontend.Services;

// Intercepta todas las respuestas HTTP; si el backend devuelve 401 (token expirado o inválido)
// cierra la sesión del cliente y redirige al login sin importar en qué página esté el usuario.
// No intenta llamar al endpoint de logout: si el token ya no es válido, la llamada fallaría.
public class AuthInterceptorHandler : DelegatingHandler
{
    private readonly TokenProvider    _token;
    private readonly NavigationManager _nav;

    public AuthInterceptorHandler(TokenProvider token, NavigationManager nav)
    {
        _token = token;
        _nav   = nav;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && _token.IsAuthenticated)
        {
            _token.ClearSession();
            _nav.NavigateTo("/login", replace: true);
        }

        return response;
    }
}
