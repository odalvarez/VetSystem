using System.Net.Http.Json;
using VetSystem.Frontend.Models;

namespace VetSystem.Frontend.Services;

public class PatientApiClient
{
    private readonly HttpClient _http;

    public PatientApiClient(HttpClient http) => _http = http;

    public async Task<PagedResponse<PatientResponse>?> ListAsync(
        int page = 1, int pageSize = 20, string? species = null, string? search = null)
    {
        var url = $"api/patients?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(species)) url += $"&species={species}";
        if (!string.IsNullOrEmpty(search))  url += $"&search={Uri.EscapeDataString(search)}";

        try { return await _http.GetFromJsonAsync<PagedResponse<PatientResponse>>(url); }
        catch { return null; }
    }

    public async Task<PatientResponse?> GetAsync(Guid id)
    {
        try { return await _http.GetFromJsonAsync<PatientResponse>($"api/patients/{id}"); }
        catch { return null; }
    }

    public async Task<PatientResponse> CreateAsync(CreatePatientRequest req)
    {
        var res = await _http.PostAsJsonAsync("api/patients", req);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadFromJsonAsync<ApiError>();
            throw new Exception(err?.Detail ?? $"Error {(int)res.StatusCode}");
        }
        return (await res.Content.ReadFromJsonAsync<PatientResponse>())!;
    }

    // Devuelve la lista plana para simplificar el uso en componentes que no necesitan paginación
    public async Task<List<ClinicalRecordResponse>?> ListRecordsAsync(
        Guid patientId, int page = 1, int pageSize = 50)
    {
        try
        {
            var paged = await _http.GetFromJsonAsync<PagedResponse<ClinicalRecordResponse>>(
                $"api/patients/{patientId}/records?page={page}&pageSize={pageSize}");
            return paged?.Items;
        }
        catch { return null; }
    }

    public async Task<ClinicalRecordResponse> AddRecordAsync(
        Guid patientId, CreateClinicalRecordRequest req)
    {
        var res = await _http.PostAsJsonAsync($"api/patients/{patientId}/records", req);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadFromJsonAsync<ApiError>();
            throw new Exception(err?.Detail ?? $"Error {(int)res.StatusCode}");
        }
        return (await res.Content.ReadFromJsonAsync<ClinicalRecordResponse>())!;
    }
}
