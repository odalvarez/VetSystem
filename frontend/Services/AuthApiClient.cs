using System.Net;
using System.Net.Http.Json;
using VetSystem.Frontend.Models;

namespace VetSystem.Frontend.Services;

public class AuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http) => _http = http;

    public async Task<LoginUserInfo> LoginAsync(LoginRequest req)
    {
        var res = await _http.PostAsJsonAsync("api/auth/login", req);

        if (res.StatusCode == HttpStatusCode.Unauthorized)
            throw new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized);

        res.EnsureSuccessStatusCode();

        var body = (await res.Content.ReadFromJsonAsync<LoginResponse>())!;
        return body.User;
    }

    public async Task LogoutAsync()
    {
        // El servidor borra la cookie httpOnly; el cliente nunca tuvo acceso al token
        try { await _http.PostAsync("api/auth/logout", null); }
        catch { /* logout best-effort: si falla el servidor, la cookie expira sola */ }
    }

    public async Task RegisterAsync(RegisterRequest req)
    {
        var res = await _http.PostAsJsonAsync("api/auth/register", req);

        if (res.StatusCode == HttpStatusCode.Conflict)
            throw new HttpRequestException("Conflict", null, HttpStatusCode.Conflict);

        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadFromJsonAsync<ApiError>();
            throw new Exception(err?.Detail ?? $"Error {(int)res.StatusCode}");
        }
    }

    // Verifica si la cookie sigue siendo válida y devuelve el perfil del usuario
    public async Task<UserProfile?> GetProfileAsync()
    {
        try { return await _http.GetFromJsonAsync<UserProfile>("api/auth/me"); }
        catch { return null; }
    }

    // Solo disponible para veterinarios; permite seleccionar el dueño al registrar una mascota
    public async Task<List<OwnerSummary>> GetOwnersAsync()
    {
        try { return await _http.GetFromJsonAsync<List<OwnerSummary>>("api/auth/owners") ?? []; }
        catch { return []; }
    }
}
