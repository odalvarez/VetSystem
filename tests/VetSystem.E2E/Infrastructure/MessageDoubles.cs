using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace VetSystem.E2E.Infrastructure;

// Consultas a Mailpit y WireMock para afirmar qué mensajes salieron de notifications-service
public sealed class MessageDoubles(E2EEnvironment env) : IDisposable
{
    private readonly HttpClient _http = new();

    public async Task<JsonArray> EmailsToAsync(string address)
    {
        var url = $"{env.MailpitUrl}/api/v1/search?query={Uri.EscapeDataString($"to:{address}")}";
        var result = await _http.GetFromJsonAsync<JsonNode>(url);
        return result!["messages"]!.AsArray();
    }

    public async Task<int> WhatsAppCallsToAsync(string number)
    {
        var resp = await _http.PostAsJsonAsync($"{env.WireMockUrl}/__admin/requests/find", new
        {
            method = "POST",
            urlPath = "/message/sendText/vetsystem",
            bodyPatterns = new[] { new { matchesJsonPath = $"$[?(@.number == '{number}')]" } }
        });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonNode>();
        return body!["requests"]!.AsArray().Count;
    }

    // Los envíos son asíncronos: se reintenta la consulta hasta que el mensaje aparezca o venza el plazo
    public static async Task<T> EventuallyAsync<T>(Func<Task<T>> probe, Func<T, bool> done, int timeoutMs = 15000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        T value;
        do
        {
            value = await probe();
            if (done(value)) return value;
            await Task.Delay(300);
        } while (DateTime.UtcNow < deadline);
        return value;
    }

    public void Dispose() => _http.Dispose();
}
