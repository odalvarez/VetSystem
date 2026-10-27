using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Data;
using AuthService.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Tests;

// Política de contraseñas, roles y forma de los errores en todos los endpoints que fijan datos de usuario
public class PasswordAndValidationEndpointTests(PolicyAuthWebFactory factory) : IClassFixture<PolicyAuthWebFactory>
{
    private const string ValidPassword  = "Valida#2026";
    private const string WeakButLong    = "abcdefgh1";
    private static readonly string TooLong = "A1!" + new string('a', 70);

    private HttpClient Anonymous => factory.CreateClient();

    private HttpClient As(Guid id, string email, string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.Generate(id, email, role));
        return client;
    }

    private HttpClient Admin => As(factory.AdminId, AuthWebFactory.AdminEmail, "Admin");

    private static RegisterRequest Owner(string password, string role = "Owner") => new()
    {
        FirstName = "Ana", LastName = "Prueba", Email = $"{Guid.NewGuid():N}@test.com",
        Password  = password, Phone = "3000000000", Role = role
    };

    private async Task<(Guid Id, string Email)> RegisterOwnerAsync()
    {
        var req  = Owner(ValidPassword);
        var resp = await Anonymous.PostAsJsonAsync("/api/auth/register", req);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<RegisterResponse>();
        return (body!.Id, req.Email);
    }

    private static async Task<string> DetailAsync(HttpResponseMessage resp) =>
        (await resp.Content.ReadFromJsonAsync<JsonNode>())!["detail"]?.GetValue<string>() ?? "";

    // ── change-password: antes no aplicaba la política ───────────────────────────

    [Fact]
    public async Task ChangePassword_WeakNewPassword_Returns400()
    {
        var (id, email) = await RegisterOwnerAsync();

        var resp = await As(id, email, "Owner").PostAsJsonAsync("/api/auth/change-password",
            new ChangePasswordRequest { CurrentPassword = ValidPassword, NewPassword = WeakButLong });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains("mayúscula", await DetailAsync(resp));
    }

    [Fact]
    public async Task ChangePassword_ValidNewPassword_AllowsLoginWithIt()
    {
        var (id, email) = await RegisterOwnerAsync();
        const string newPassword = "Distinta#2027";

        var resp = await As(id, email, "Owner").PostAsJsonAsync("/api/auth/change-password",
            new ChangePasswordRequest { CurrentPassword = ValidPassword, NewPassword = newPassword });
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var login = await Anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = newPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    // ── 72 bytes: BCrypt ignoraría el resto ─────────────────────────────────────

    [Fact]
    public async Task Register_PasswordOver72Bytes_Returns400() =>
        Assert.Equal(HttpStatusCode.BadRequest,
            (await Anonymous.PostAsJsonAsync("/api/auth/register", Owner(TooLong))).StatusCode);

    [Fact]
    public async Task AdminCreate_PasswordOver72Bytes_Returns400()
    {
        var resp = await Admin.PostAsJsonAsync("/api/auth/admin/users", new AdminCreateUserRequest
        {
            FirstName = "Ana", LastName = "Prueba", Email = $"{Guid.NewGuid():N}@test.com",
            Password = TooLong, Role = "Veterinarian"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_NewPasswordOver72Bytes_Returns400()
    {
        var (id, email) = await RegisterOwnerAsync();
        var resp = await As(id, email, "Owner").PostAsJsonAsync("/api/auth/change-password",
            new ChangePasswordRequest { CurrentPassword = ValidPassword, NewPassword = TooLong });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task AdminReset_WeakOrTooLongPassword_Returns400()
    {
        var (id, _) = await RegisterOwnerAsync();

        foreach (var password in new[] { WeakButLong, TooLong })
        {
            var resp = await Admin.PostAsJsonAsync($"/api/auth/admin/users/{id}/reset-password",
                new AdminResetPasswordRequest { NewPassword = password });
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }
    }

    // ── Roles: solo por nombre ──────────────────────────────────────────────────

    [Theory]
    [InlineData("1")]
    [InlineData("7")]
    public async Task Register_NumericRole_Returns400(string role) =>
        Assert.Equal(HttpStatusCode.BadRequest,
            (await Anonymous.PostAsJsonAsync("/api/auth/register", Owner(ValidPassword, role))).StatusCode);

    [Theory]
    [InlineData("7")]
    [InlineData("Superadmin")]
    public async Task AdminCreate_UnknownRole_Returns400(string role)
    {
        var resp = await Admin.PostAsJsonAsync("/api/auth/admin/users", new AdminCreateUserRequest
        {
            FirstName = "Ana", LastName = "Prueba", Email = $"{Guid.NewGuid():N}@test.com",
            Password = ValidPassword, Role = role
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task AdminUpdate_NumericRole_Returns400()
    {
        var (id, _) = await RegisterOwnerAsync();
        var resp = await Admin.PutAsJsonAsync($"/api/auth/admin/users/{id}",
            new AdminUpdateUserRequest { FirstName = "Ana", LastName = "Prueba", Role = "7" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task AdminUpdate_EmptyName_Returns400()
    {
        var (id, _) = await RegisterOwnerAsync();
        var resp = await Admin.PutAsJsonAsync($"/api/auth/admin/users/{id}",
            new AdminUpdateUserRequest { FirstName = "", LastName = "Prueba", Role = "Owner" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Forma única de error ────────────────────────────────────────────────────

    [Fact]
    public async Task AttributeValidationError_HasSpanishDetailAndFieldErrors()
    {
        var req = Owner(ValidPassword);
        req.Email = "no-es-un-correo";

        var resp = await Anonymous.PostAsJsonAsync("/api/auth/register", req);
        var body = await resp.Content.ReadFromJsonAsync<JsonNode>();

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("application/problem+json", resp.Content.Headers.ContentType?.MediaType);
        Assert.Contains("correo", body!["detail"]!.GetValue<string>());
        Assert.NotNull(body["errors"]?["Email"]);
        Assert.Equal("/api/auth/register", body["instance"]!.GetValue<string>());
    }

    // ── Regresión: la política no aplica al login ───────────────────────────────

    [Fact]
    public async Task Login_UserWithLegacyWeakPassword_StillSignsIn()
    {
        var email = $"legacy_{Guid.NewGuid():N}@test.com";
        using (var scope = factory.Services.CreateScope())
        {
            var db     = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            db.Users.Add(User.Create("Legado", "Débil", email, hasher.Hash("abc12345"), "", UserRole.Owner));
            await db.SaveChangesAsync();
        }

        var resp = await Anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "abc12345" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
