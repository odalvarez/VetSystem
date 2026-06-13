using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuthService.Application.DTOs;
using AuthService.Tests.Helpers;

namespace AuthService.Tests;

public class AuthControllerTests : IClassFixture<AuthWebFactory>
{
    private readonly HttpClient    _client;
    private readonly AuthWebFactory _factory;

    public AuthControllerTests(AuthWebFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidOwner_Returns201()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Juan",
            LastName  = "Perez",
            Email     = $"owner_{Guid.NewGuid():N}@test.com",
            Password  = "Password1!",
            Phone     = "3001234567",
            Role      = "Owner"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = $"dup_{Guid.NewGuid():N}@test.com";
        var req   = new RegisterRequest
        {
            FirstName = "Maria",
            LastName  = "Lopez",
            Email     = email,
            Password  = "Password1!",
            Phone     = "3001234567",
            Role      = "Owner"
        };

        await _client.PostAsJsonAsync("/api/auth/register", req);
        var resp2 = await _client.PostAsJsonAsync("/api/auth/register", req);

        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidPassword_Returns400()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Ana",
            LastName  = "Gil",
            Email     = "short@test.com",
            Password  = "abc",
            Phone     = "3001234567",
            Role      = "Owner"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200AndSetsCookie()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email    = AuthWebFactory.AdminEmail,
            Password = AuthWebFactory.AdminPassword
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.True(resp.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, c => c.Contains("vetsys_jwt"));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email    = AuthWebFactory.AdminEmail,
            Password = "wrongpassword"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email    = "noexiste@test.com",
            Password = "Password1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Me ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        var resp = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetMe_AuthenticatedAdmin_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                JwtTestHelper.Generate(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin"));

        var resp = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Owners endpoint ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetOwners_AsOwner_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                JwtTestHelper.Generate(_factory.OwnerId, "owner@test.com", "Owner"));

        var resp = await client.GetAsync("/api/auth/owners");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task GetOwners_AsVet_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                JwtTestHelper.Generate(_factory.VetId, "vet@test.com", "Veterinarian"));

        var resp = await client.GetAsync("/api/auth/owners");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetOwners_AsAdmin_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                JwtTestHelper.Generate(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin"));

        var resp = await client.GetAsync("/api/auth/owners");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Admin endpoints ───────────────────────────────────────────────────────

    [Fact]
    public async Task AdminListUsers_AsAdmin_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                JwtTestHelper.Generate(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin"));

        var resp = await client.GetAsync("/api/auth/admin/users");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task AdminListUsers_AsVet_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                JwtTestHelper.Generate(_factory.VetId, "vet@test.com", "Veterinarian"));

        var resp = await client.GetAsync("/api/auth/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task AdminCreateUser_AsAdmin_Returns201()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                JwtTestHelper.Generate(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin"));

        var resp = await client.PostAsJsonAsync("/api/auth/admin/users", new
        {
            FirstName = "Nuevo",
            LastName  = "Vet",
            Email     = $"newvet_{Guid.NewGuid():N}@test.com",
            Password  = "Test123#",
            Phone     = "3009999999",
            Role      = "Veterinarian"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_Returns204()
    {
        var resp = await _client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }
}
