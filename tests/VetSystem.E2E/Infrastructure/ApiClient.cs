using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace VetSystem.E2E.Infrastructure;

// Cliente HTTP para preparar datos por API: la UI se prueba en los tests, no en el arranque
public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;

    public ApiClient(string baseUrl)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            // El stack E2E usa un certificado autofirmado generado en cada corrida
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        _http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<JsonNode> LoginAsync(string email, string password)
    {
        var resp = await _http.PostAsJsonAsync("/api/auth/login", new { email, password });
        await EnsureAsync(resp, "login");
        return (await resp.Content.ReadFromJsonAsync<JsonNode>())!["user"]!;
    }

    public async Task<Guid> CreateUserAsync(string firstName, string lastName, string email, string password, string phone, string role)
    {
        var resp = await _http.PostAsJsonAsync("/api/auth/admin/users",
            new { firstName, lastName, email, password, phone, role });
        await EnsureAsync(resp, $"crear usuario {role}");
        return (await resp.Content.ReadFromJsonAsync<JsonNode>())!["id"]!.GetValue<Guid>();
    }

    public async Task SetClinicHoursAsync(string start, string end, params string[] workDays)
    {
        var resp = await _http.PutAsJsonAsync("/api/schedules/settings", new { startTime = start, endTime = end, workDays });
        await EnsureAsync(resp, "horario de la clínica");
    }

    public async Task<Guid> CreatePatientAsync(Guid ownerId, string ownerName, string ownerPhone, string name, string species = "dog")
    {
        var resp = await _http.PostAsJsonAsync("/api/patients", new
        {
            name, species, breed = "Mestizo", birthDate = DateTime.Today.AddYears(-3).ToString("yyyy-MM-dd"),
            sex = "female", weightKg = 12.5, color = "Café", microchipNumber = (string?)null,
            ownerId, ownerName, ownerPhone
        });
        await EnsureAsync(resp, "crear mascota");
        return (await resp.Content.ReadFromJsonAsync<JsonNode>())!["id"]!.GetValue<Guid>();
    }

    // El catálogo de vacunas no tiene UI de administración; se siembra por API una sola vez por stack
    public async Task EnsureVaccineAsync(string name)
    {
        var existing = await GetJsonArrayAsync("/api/vaccines");
        if (existing.Any(v => v!["name"]!.GetValue<string>() == name)) return;

        var resp = await _http.PostAsJsonAsync("/api/vaccines",
            new { name, description = (string?)null, scheme = "Annual", annualIntervalMonths = 12, doseSteps = (object?)null });
        await EnsureAsync(resp, "crear vacuna");
    }

    public async Task<JsonArray> GetJsonArrayAsync(string url)
    {
        var resp = await _http.GetAsync(url);
        await EnsureAsync(resp, url);
        var node = await resp.Content.ReadFromJsonAsync<JsonNode>();
        return node as JsonArray ?? node!["items"]!.AsArray();
    }

    private static async Task EnsureAsync(HttpResponseMessage resp, string step)
    {
        if (resp.IsSuccessStatusCode) return;
        var body = await resp.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Preparación E2E falló en '{step}': {(int)resp.StatusCode} {body}");
    }

    public void Dispose() => _http.Dispose();
}
