using System.Net.Http.Json;
using VetSystem.Frontend.Models;

namespace VetSystem.Frontend.Services;

public class ConsultationLogApiClient
{
    private readonly HttpClient _http;

    public ConsultationLogApiClient(HttpClient http) => _http = http;

    public async Task<PagedResponse<ConsultationLogResponse>> ListAsync(
        Guid patientId, int page = 1, int pageSize = 20)
    {
        try
        {
            return await _http.GetFromJsonAsync<PagedResponse<ConsultationLogResponse>>(
                $"api/patients/{patientId}/logs?page={page}&pageSize={pageSize}")
                ?? new();
        }
        catch { return new(); }
    }

    public async Task<ConsultationLogResponse> CreateAsync(
        Guid patientId, CreateConsultationLogRequest req)
    {
        var resp = await _http.PostAsJsonAsync($"api/patients/{patientId}/logs", req);
        await ThrowIfErrorAsync(resp);
        return (await resp.Content.ReadFromJsonAsync<ConsultationLogResponse>())!;
    }

    public async Task<ConsultationLogResponse> UpdateAsync(
        Guid patientId, Guid logId, CreateConsultationLogRequest req)
    {
        var resp = await _http.PutAsJsonAsync($"api/patients/{patientId}/logs/{logId}", req);
        await ThrowIfErrorAsync(resp);
        return (await resp.Content.ReadFromJsonAsync<ConsultationLogResponse>())!;
    }

    public async Task<ConsultationLogResponse?> GetByAppointmentAsync(Guid patientId, Guid appointmentId)
    {
        try
        {
            var resp = await _http.GetAsync($"api/patients/{patientId}/logs/by-appointment/{appointmentId}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            await ThrowIfErrorAsync(resp);
            return await resp.Content.ReadFromJsonAsync<ConsultationLogResponse>();
        }
        catch (Exception ex) when (ex.Message.StartsWith("Error ")) { return null; }
    }

    public async Task<ConsultationLogResponse> CloseAsync(Guid patientId, Guid logId)
    {
        var resp = await _http.PatchAsJsonAsync(
            $"api/patients/{patientId}/logs/{logId}/close", new { });
        await ThrowIfErrorAsync(resp);
        return (await resp.Content.ReadFromJsonAsync<ConsultationLogResponse>())!;
    }

    private static async Task ThrowIfErrorAsync(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode) return;
        string detail;
        try
        {
            var body = await resp.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            detail = doc.RootElement.TryGetProperty("detail", out var d)
                ? d.GetString() ?? body : body;
        }
        catch { detail = $"Error {(int)resp.StatusCode}"; }
        throw new Exception(detail);
    }
}
