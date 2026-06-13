using System.Net.Http.Json;
using VetSystem.Frontend.Models;

namespace VetSystem.Frontend.Services;

public class AppointmentApiClient
{
    private readonly HttpClient _http;

    public AppointmentApiClient(HttpClient http) => _http = http;

    public async Task<List<AppointmentResponse>?> ListAsync(
        string? status = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = 100)
    {
        var paged = await ListPagedAsync(status, from, to, page, pageSize);
        return paged?.Items;
    }

    public async Task<PagedResponse<AppointmentResponse>?> ListPagedAsync(
        string? status = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = 20)
    {
        var url = $"api/appointments?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status)) url += $"&status={status}";
        if (from.HasValue) url += $"&from={from.Value:yyyy-MM-dd}";
        if (to.HasValue)   url += $"&to={to.Value:yyyy-MM-dd}";

        try { return await _http.GetFromJsonAsync<PagedResponse<AppointmentResponse>>(url); }
        catch { return null; }
    }

    public async Task<AppointmentResponse> CreateAsync(CreateAppointmentRequest req)
    {
        var res = await _http.PostAsJsonAsync("api/appointments", req);
        if (!res.IsSuccessStatusCode)
            throw new Exception(await ParseError(res));
        return (await res.Content.ReadFromJsonAsync<AppointmentResponse>())!;
    }

    public async Task ChangeStatusAsync(Guid id, string newStatus)
    {
        var res = await _http.PatchAsJsonAsync(
            $"api/appointments/{id}/status", new { Status = newStatus });
        if (!res.IsSuccessStatusCode)
            throw new Exception(await ParseError(res));
    }

    // Extrae el mensaje de error tanto del formato ProblemDetails (detail)
    // como del formato de validación de ASP.NET Core (errors)
    private static async Task<string> ParseError(HttpResponseMessage res)
    {
        try
        {
            var body = await res.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("detail", out var d) && d.GetString() is { Length: > 0 } detail)
                return detail;

            if (root.TryGetProperty("errors", out var errors))
            {
                var messages = new List<string>();
                foreach (var field in errors.EnumerateObject())
                    foreach (var msg in field.Value.EnumerateArray())
                        if (msg.GetString() is { } m) messages.Add(m);
                if (messages.Count > 0) return string.Join(" ", messages);
            }
        }
        catch { }
        return $"Error {(int)res.StatusCode}";
    }

}
