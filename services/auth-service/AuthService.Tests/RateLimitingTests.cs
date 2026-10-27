using System.Net;
using System.Net.Http.Json;
using AuthService.Application.DTOs;
using AuthService.Tests.Helpers;

namespace AuthService.Tests;

public class RateLimitingTests(RateLimitedAuthWebFactory factory) : IClassFixture<RateLimitedAuthWebFactory>
{
    private HttpClient ClientFrom(string ip)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", ip);
        return client;
    }

    private static Task<HttpResponseMessage> WrongLoginAsync(HttpClient client) =>
        client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = "nadie@test.com", Password = "Incorrecta#1" });

    [Fact]
    public async Task Login_ExceedingLimitFromSameClient_Returns429()
    {
        var client = ClientFrom("203.0.113.10");

        for (var i = 0; i < RateLimitedAuthWebFactory.LoginLimit; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await WrongLoginAsync(client)).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await WrongLoginAsync(client)).StatusCode);
    }

    [Fact]
    public async Task Login_LimitIsPerClient_OtherClientIsNotBlocked()
    {
        // Antes el límite era global: que un cliente lo agotara bloqueaba el login de todos
        var noisy = ClientFrom("203.0.113.30");
        for (var i = 0; i <= RateLimitedAuthWebFactory.LoginLimit; i++)
            await WrongLoginAsync(noisy);

        var other = ClientFrom("203.0.113.40");
        Assert.Equal(HttpStatusCode.Unauthorized, (await WrongLoginAsync(other)).StatusCode);
    }

    [Fact]
    public async Task Register_ExceedingLimitFromSameClient_Returns429()
    {
        var client = ClientFrom("203.0.113.50");
        RegisterRequest NewOwner() => new()
        {
            FirstName = "Ana", LastName = "Prueba", Email = $"{Guid.NewGuid():N}@test.com",
            Password = "Valida#2026", Phone = "3000000000", Role = "Owner"
        };

        for (var i = 0; i < RateLimitedAuthWebFactory.RegisterLimit; i++)
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", NewOwner())).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsJsonAsync("/api/auth/register", NewOwner())).StatusCode);
    }
}
