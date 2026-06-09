using System.Net.Http.Json;
using System.Text.Json;
using VetSystem.Frontend.Models;

namespace VetSystem.Frontend.Services;

public class SpeciesApiClient
{
    private readonly HttpClient _http;

    public SpeciesApiClient(HttpClient http) => _http = http;

    public async Task<List<SpeciesResponse>> GetAllAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<SpeciesResponse>>("api/species")
                   ?? [];
        }
        catch { return []; }
    }

    public async Task<SpeciesResponse> CreateAsync(CreateSpeciesRequest req)
    {
        var resp = await _http.PostAsJsonAsync("api/species", req);
        await ThrowIfErrorAsync(resp);
        return (await resp.Content.ReadFromJsonAsync<SpeciesResponse>())!;
    }

    public async Task<SpeciesResponse> UpdateAsync(Guid id, UpdateSpeciesRequest req)
    {
        var resp = await _http.PutAsJsonAsync($"api/species/{id}", req);
        await ThrowIfErrorAsync(resp);
        return (await resp.Content.ReadFromJsonAsync<SpeciesResponse>())!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var resp = await _http.DeleteAsync($"api/species/{id}");
        await ThrowIfErrorAsync(resp);
    }

    // Lee el cuerpo del error para mostrar el mensaje real en lugar del genérico "400 Bad Request"
    private static async Task ThrowIfErrorAsync(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode) return;

        string detail;
        try
        {
            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            detail = doc.RootElement.TryGetProperty("detail", out var d)
                ? d.GetString() ?? body
                : body;
        }
        catch
        {
            detail = $"Error {(int)resp.StatusCode}";
        }

        throw new Exception(detail);
    }
}
