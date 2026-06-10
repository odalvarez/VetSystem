using System.Net.Http.Json;
using VetSystem.Frontend.Models;

namespace VetSystem.Frontend.Services;

public class NotificationApiClient
{
    private readonly HttpClient _http;

    public NotificationApiClient(HttpClient http) => _http = http;

    // phone: solo se envía cuando el usuario es Owner para filtrar sus notificaciones por WhatsApp
    public async Task<List<NotificationStatusResponse>> ListAllAsync(
        int page = 1, int pageSize = 50, string? phone = null)
    {
        var url = $"api/notifications?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(phone))
            url += $"&phone={Uri.EscapeDataString(phone)}";

        try { return await _http.GetFromJsonAsync<List<NotificationStatusResponse>>(url) ?? []; }
        catch { return []; }
    }

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
}
