using System.Net;
using System.Net.Http.Json;
using AuthService.Application.DTOs;
using AuthService.Tests.Helpers;

namespace AuthService.Tests;

public class ForwardedHeadersSpoofingTests(InternetClientAuthWebFactory factory) : IClassFixture<InternetClientAuthWebFactory>
{
    [Fact]
    public async Task Login_XForwardedForFromUntrustedClient_DoesNotBypassLimit()
    {
        // Si se confiara en la cabecera de cualquier origen, rotar IPs falsas evitaría el límite
        var client = factory.CreateClient();
        var login  = new LoginRequest { Email = "nadie@test.com", Password = "Incorrecta#1" };

        for (var i = 0; i < RateLimitedAuthWebFactory.LoginLimit; i++)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(login) };
            req.Headers.Add("X-Forwarded-For", $"203.0.113.{100 + i}");
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(req)).StatusCode);
        }

        var spoofed = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(login) };
        spoofed.Headers.Add("X-Forwarded-For", "203.0.113.250");
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.SendAsync(spoofed)).StatusCode);
    }
}
