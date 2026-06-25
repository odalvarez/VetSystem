using System.Net.Http.Json;
using VetSystem.Frontend.Models;

namespace VetSystem.Frontend.Services;

public class VaccinationApiClient(HttpClient http)
{
    public async Task<List<VaccineDefinitionResponse>> ListDefinitionsAsync()
    {
        try { return await http.GetFromJsonAsync<List<VaccineDefinitionResponse>>("api/vaccines") ?? []; }
        catch { return []; }
    }

    public async Task<VaccineDefinitionResponse?> CreateDefinitionAsync(CreateVaccineDefinitionRequest req)
    {
        var resp = await http.PostAsJsonAsync("api/vaccines", req);
        return resp.IsSuccessStatusCode
            ? await resp.Content.ReadFromJsonAsync<VaccineDefinitionResponse>()
            : null;
    }

    public async Task<List<VaccinationRecordResponse>> ListByPatientAsync(Guid patientId)
    {
        try { return await http.GetFromJsonAsync<List<VaccinationRecordResponse>>($"api/patients/{patientId}/vaccinations") ?? []; }
        catch { return []; }
    }

    public async Task<VaccinationRecordResponse?> RegisterAsync(Guid patientId, RegisterVaccinationRequest req)
    {
        var resp = await http.PostAsJsonAsync($"api/patients/{patientId}/vaccinations", req);
        return resp.IsSuccessStatusCode
            ? await resp.Content.ReadFromJsonAsync<VaccinationRecordResponse>()
            : null;
    }

    public async Task<bool> DeleteAsync(Guid patientId, Guid recordId)
    {
        var resp = await http.DeleteAsync($"api/patients/{patientId}/vaccinations/{recordId}");
        return resp.IsSuccessStatusCode;
    }
}
