using System.Net.Http.Json;
using VetSystem.Frontend.Models;

namespace VetSystem.Frontend.Services;

public class NotificationApiClient
{
    private readonly HttpClient _http;

    public NotificationApiClient(HttpClient http) => _http = http;

    public async Task<NotificationStatusResponse?> GetStatusAsync(Guid appointmentId)
    {
        try { return await _http.GetFromJsonAsync<NotificationStatusResponse>($"api/notifications/{appointmentId}"); }
        catch { return null; }
    }

    public async Task SendWhatsAppAsync(SendWhatsAppRequest req)
    {
        var res = await _http.PostAsJsonAsync("api/notifications/whatsapp", req);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadFromJsonAsync<ApiError>();
            throw new Exception(err?.Detail ?? $"Error {(int)res.StatusCode}");
        }
    }

    public async Task SendEmailAsync(SendEmailRequest req)
    {
        var res = await _http.PostAsJsonAsync("api/notifications/email", req);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadFromJsonAsync<ApiError>();
            throw new Exception(err?.Detail ?? $"Error {(int)res.StatusCode}");
        }
    }

    public async Task ScheduleReminderAsync(ScheduleReminderRequest req)
    {
        var res = await _http.PostAsJsonAsync("api/notifications/reminder", req);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadFromJsonAsync<ApiError>();
            throw new Exception(err?.Detail ?? $"Error {(int)res.StatusCode}");
        }
    }
}
