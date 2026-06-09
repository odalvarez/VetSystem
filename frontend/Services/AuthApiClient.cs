using System.Net;
using System.Net.Http.Json;
using VetSystem.Frontend.Models;

namespace VetSystem.Frontend.Services;

public class AuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http) => _http = http;

    public async Task<LoginResponse> LoginAsync(LoginRequest req)
    {
        var res = await _http.PostAsJsonAsync("api/auth/login", req);

        if (res.StatusCode == HttpStatusCode.Unauthorized)
            throw new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized);

        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<LoginResponse>())!;
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

    public async Task<UserProfile?> GetProfileAsync()
    {
        try { return await _http.GetFromJsonAsync<UserProfile>("api/auth/me"); }
        catch { return null; }
    }
}
