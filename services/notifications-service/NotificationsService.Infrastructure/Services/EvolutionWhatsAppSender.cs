using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NotificationsService.Application.Interfaces;

namespace NotificationsService.Infrastructure.Services;

public class EvolutionWhatsAppSender : IWhatsAppSender
{
    private readonly HttpClient     _http;
    private readonly string         _instanceName;
    private readonly string         _apiKey;

    public EvolutionWhatsAppSender(HttpClient http, IConfiguration config)
    {
        _http         = http;
        _instanceName = config["EvolutionApi:InstanceName"] ?? "vetsystem";
        _apiKey       = config["EvolutionApi:ApiKey"]       ?? "";

        _http.BaseAddress = new Uri(
            config["EvolutionApi:BaseUrl"] ?? "http://evolution-api:8080");
        _http.DefaultRequestHeaders.Add("apikey", _apiKey);
    }

    public async Task SendAsync(string to, string message, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            number = to,
            text   = message,
            delay  = 1200
        });

        var response = await _http.PostAsync(
            $"/message/sendText/{_instanceName}",
            new StringContent(payload, Encoding.UTF8, "application/json"),
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new Exception($"Evolution API error {(int)response.StatusCode}: {body}");
        }
    }
}
