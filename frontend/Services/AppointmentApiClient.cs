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
        var url = $"api/appointments?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status)) url += $"&status={status}";
        if (from.HasValue) url += $"&from={from.Value:yyyy-MM-dd}";
        if (to.HasValue)   url += $"&to={to.Value:yyyy-MM-dd}";

        try
        {
            var paged = await _http.GetFromJsonAsync<PagedResponse<AppointmentResponse>>(url);
            return paged?.Items;
        }
        catch { return null; }
    }

    public async Task<AppointmentResponse> CreateAsync(CreateAppointmentRequest req)
    {
        var res = await _http.PostAsJsonAsync("api/appointments", req);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadFromJsonAsync<ApiError>();
            throw new Exception(err?.Detail ?? $"Error {(int)res.StatusCode}");
        }
        return (await res.Content.ReadFromJsonAsync<AppointmentResponse>())!;
    }

    public async Task ChangeStatusAsync(Guid id, string newStatus)
    {
        var res = await _http.PatchAsJsonAsync(
            $"api/appointments/{id}/status", new { Status = newStatus });
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadFromJsonAsync<ApiError>();
            throw new Exception(err?.Detail ?? $"Error {(int)res.StatusCode}");
        }
    }

    public async Task<List<AvailabilityResponse>?> GetAvailabilityAsync(
        Guid veterinarianId, DateTime date)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<AvailabilityResponse>>(
                $"api/appointments/availability?veterinarianId={veterinarianId}&date={date:yyyy-MM-dd}");
        }
        catch { return null; }
    }
}
