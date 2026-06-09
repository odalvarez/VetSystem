namespace VetSystem.Frontend.Services;

// Se inyecta en el pipeline de todos los HttpClients tipados.
// Así no tenemos que poner el header en cada llamada individualmente.
public class AuthTokenHandler : DelegatingHandler
{
    private readonly TokenProvider _tokenProvider;

    public AuthTokenHandler(TokenProvider tokenProvider) => _tokenProvider = tokenProvider;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_tokenProvider.IsAuthenticated)
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenProvider.Token);

        return await base.SendAsync(request, cancellationToken);
    }
}
