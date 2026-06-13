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

    // Lista de veterinarios activos para el selector al agendar citas
    public async Task<List<VetSummary>> GetVeterinariansAsync()
    {
        try { return await _http.GetFromJsonAsync<List<VetSummary>>("api/auth/veterinarians") ?? []; }
        catch { return []; }
    }

    // ── Admin ─────────────────────────────────────────────────────────────────────

    public async Task<AdminPagedUsers> AdminListUsersAsync(
        string? role = null, string? search = null, bool? isActive = null,
        int page = 1, int pageSize = 15)
    {
        var url = $"api/auth/admin/users?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(role))   url += $"&role={role}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
        if (isActive.HasValue)             url += $"&isActive={isActive.Value.ToString().ToLower()}";
        try { return await _http.GetFromJsonAsync<AdminPagedUsers>(url) ?? new(); }
        catch { return new(); }
    }

    public async Task<AdminUserItem> AdminCreateUserAsync(AdminCreateUserRequest req)
    {
        var res = await _http.PostAsJsonAsync("api/auth/admin/users", req);
        if (res.StatusCode == HttpStatusCode.Conflict)
            throw new HttpRequestException("Ya existe un usuario con ese correo.", null, HttpStatusCode.Conflict);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<AdminUserItem>())!;
    }

    public async Task<AdminUserItem> AdminUpdateUserAsync(Guid id, AdminUpdateUserRequest req)
    {
        var res = await _http.PutAsJsonAsync($"api/auth/admin/users/{id}", req);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<AdminUserItem>())!;
    }

    public async Task AdminSetActiveAsync(Guid id, bool active)
    {
        var res = await _http.PatchAsync($"api/auth/admin/users/{id}/active?value={active.ToString().ToLower()}", null);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadFromJsonAsync<ApiError>();
            throw new Exception(err?.Detail ?? $"Error {(int)res.StatusCode}");
        }
    }

    public async Task AdminResetPasswordAsync(Guid id, string newPassword)
    {
        var res = await _http.PostAsJsonAsync($"api/auth/admin/users/{id}/reset-password",
            new AdminResetPasswordRequest { NewPassword = newPassword });
        res.EnsureSuccessStatusCode();
    }

    public async Task AdminDeleteUserAsync(Guid id)
    {
        var res = await _http.DeleteAsync($"api/auth/admin/users/{id}");
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadFromJsonAsync<ApiError>();
            throw new Exception(err?.Detail ?? $"Error {(int)res.StatusCode}");
        }
    }
}
