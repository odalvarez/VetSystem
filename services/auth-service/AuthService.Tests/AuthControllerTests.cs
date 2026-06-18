using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using AuthService.Application.DTOs;
using AuthService.Tests.Helpers;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Tests;

public class AuthControllerTests : IClassFixture<AuthWebFactory>
{
    private readonly HttpClient     _client;
    private readonly AuthWebFactory _factory;

    public AuthControllerTests(AuthWebFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    private HttpClient ClientAs(Guid userId, string email, string role) =>
        AuthorizedClient(JwtTestHelper.Generate(userId, email, role));

    private HttpClient AuthorizedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
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

    // ── Logout ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_Returns204()
    {
        var resp = await _client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
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
        var client = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var resp   = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── UpdateMe ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMe_ValidData_Returns200()
    {
        var client = ClientAs(_factory.OwnerId, "owner@test.com", "Owner");
        var resp   = await client.PutAsJsonAsync("/api/auth/me", new UpdateProfileRequest
        {
            FirstName = "Nuevo",
            LastName  = "Nombre",
            Phone     = "3009990000"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_Unauthenticated_Returns401()
    {
        var resp = await _client.PutAsJsonAsync("/api/auth/me", new UpdateProfileRequest
        {
            FirstName = "X",
            LastName  = "Y",
            Phone     = "3000000000"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── ChangePassword ────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_CorrectCurrent_Returns204()
    {
        var client = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var resp   = await client.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = AuthWebFactory.AdminPassword,
            NewPassword     = "NewPass123!"
        });

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_Returns401()
    {
        var client = ClientAs(_factory.VetId, "vet@test.com", "Veterinarian");
        var resp   = await client.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = "wrongpassword",
            NewPassword     = "NewPass123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WeakNew_Returns400()
    {
        var client = ClientAs(_factory.VetId, "vet@test.com", "Veterinarian");
        var resp   = await client.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = AuthWebFactory.AdminPassword,
            NewPassword     = "abc"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Owners ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOwners_AsVet_Returns200()
    {
        var client = ClientAs(_factory.VetId, "vet@test.com", "Veterinarian");
        var resp   = await client.GetAsync("/api/auth/owners");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetOwners_AsAdmin_Returns200()
    {
        var client = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var resp   = await client.GetAsync("/api/auth/owners");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetOwners_AsOwner_Returns403()
    {
        var client = ClientAs(_factory.OwnerId, "owner@test.com", "Owner");
        var resp   = await client.GetAsync("/api/auth/owners");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── Veterinarians ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetVeterinarians_Authenticated_Returns200()
    {
        var client = ClientAs(_factory.OwnerId, "owner@test.com", "Owner");
        var resp   = await client.GetAsync("/api/auth/veterinarians");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetVeterinarians_Unauthenticated_Returns401()
    {
        var resp = await _client.GetAsync("/api/auth/veterinarians");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Admin: listar usuarios ────────────────────────────────────────────────

    [Fact]
    public async Task AdminListUsers_AsAdmin_Returns200()
    {
        var client = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var resp   = await client.GetAsync("/api/auth/admin/users");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task AdminListUsers_AsVet_Returns403()
    {
        var client = ClientAs(_factory.VetId, "vet@test.com", "Veterinarian");
        var resp   = await client.GetAsync("/api/auth/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task AdminListUsers_AsOwner_Returns403()
    {
        var client = ClientAs(_factory.OwnerId, "owner@test.com", "Owner");
        var resp   = await client.GetAsync("/api/auth/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── Admin: obtener usuario por ID ────────────────────────────────────────

    [Fact]
    public async Task AdminGetUser_AsAdmin_Returns200()
    {
        var client = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var resp   = await client.GetAsync($"/api/auth/admin/users/{_factory.VetId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task AdminGetUser_AsVet_Returns403()
    {
        var client = ClientAs(_factory.VetId, "vet@test.com", "Veterinarian");
        var resp   = await client.GetAsync($"/api/auth/admin/users/{_factory.OwnerId}");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── Admin: crear usuario ──────────────────────────────────────────────────

    [Fact]
    public async Task AdminCreateUser_AsAdmin_Returns201()
    {
        var client = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var resp   = await client.PostAsJsonAsync("/api/auth/admin/users", new AdminCreateUserRequest
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

    // ── Admin: actualizar usuario ─────────────────────────────────────────────

    [Fact]
    public async Task AdminUpdateUser_AsAdmin_Returns200()
    {
        var client = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var resp   = await client.PutAsJsonAsync($"/api/auth/admin/users/{_factory.VetId}", new AdminUpdateUserRequest
        {
            FirstName = "Vet",
            LastName  = "Actualizado",
            Phone     = "3001112222",
            Role      = "Veterinarian"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task AdminUpdateUser_AsVet_Returns403()
    {
        var client = ClientAs(_factory.VetId, "vet@test.com", "Veterinarian");
        var resp   = await client.PutAsJsonAsync($"/api/auth/admin/users/{_factory.OwnerId}", new AdminUpdateUserRequest
        {
            FirstName = "Hack",
            LastName  = "Attempt",
            Phone     = "0000000000",
            Role      = "Admin"
        });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── Admin: activar/desactivar usuario ────────────────────────────────────

    [Fact]
    public async Task AdminSetActive_AsAdmin_Returns204()
    {
        var client = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var resp   = await client.PatchAsync($"/api/auth/admin/users/{_factory.OwnerId}/active?value=false", null);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task AdminSetActive_AsVet_Returns403()
    {
        var client = ClientAs(_factory.VetId, "vet@test.com", "Veterinarian");
        var resp   = await client.PatchAsync($"/api/auth/admin/users/{_factory.OwnerId}/active?value=false", null);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task AdminSetActive_OwnAccount_Returns400()
    {
        var client = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var resp   = await client.PatchAsync($"/api/auth/admin/users/{_factory.AdminId}/active?value=false", null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Admin: reset password ────────────────────────────────────────────────

    [Fact]
    public async Task AdminResetPassword_AsAdmin_Returns204()
    {
        var client = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var resp   = await client.PostAsJsonAsync(
            $"/api/auth/admin/users/{_factory.VetId}/reset-password",
            new AdminResetPasswordRequest { NewPassword = "Reset123!" });

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task AdminResetPassword_AsOwner_Returns403()
    {
        var client = ClientAs(_factory.OwnerId, "owner@test.com", "Owner");
        var resp   = await client.PostAsJsonAsync(
            $"/api/auth/admin/users/{_factory.VetId}/reset-password",
            new AdminResetPasswordRequest { NewPassword = "Hack123!" });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── Admin: eliminar usuario (soft delete) ────────────────────────────────

    [Fact]
    public async Task AdminDeleteUser_AsAdmin_Returns204()
    {
        var adminClient = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var create = await adminClient.PostAsJsonAsync("/api/auth/admin/users", new AdminCreateUserRequest
        {
            FirstName = "Tmp",
            LastName  = "User",
            Email     = $"to_delete_{Guid.NewGuid():N}@test.com",
            Password  = "Password1!",
            Phone     = "3001234567",
            Role      = "Owner"
        });
        var created = await create.Content.ReadFromJsonAsync<AdminUserItem>();

        var resp = await adminClient.DeleteAsync($"/api/auth/admin/users/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task AdminDeleteUser_OwnAccount_Returns400()
    {
        var client = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var resp   = await client.DeleteAsync($"/api/auth/admin/users/{_factory.AdminId}");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task AdminDeleteUser_AsVet_Returns403()
    {
        var client = ClientAs(_factory.VetId, "vet@test.com", "Veterinarian");
        var resp   = await client.DeleteAsync($"/api/auth/admin/users/{_factory.OwnerId}");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task AdminDeleteUser_Unauthenticated_Returns401()
    {
        var resp = await _client.DeleteAsync($"/api/auth/admin/users/{_factory.OwnerId}");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── P1: seguridad y estados de cuenta ────────────────────────────────────

    [Fact]
    public async Task Register_AsAdmin_Role_Returns400()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Hacker",
            LastName  = "Admin",
            Email     = $"hack_admin_{Guid.NewGuid():N}@test.com",
            Password  = "Password1!",
            Phone     = "3001234567",
            Role      = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Register_AsVeterinarian_Role_Returns400()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Hacker",
            LastName  = "Vet",
            Email     = $"hack_vet_{Guid.NewGuid():N}@test.com",
            Password  = "Password1!",
            Phone     = "3001234567",
            Role      = "Veterinarian"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Login_InactiveAccount_Returns401()
    {
        // Registra un usuario nuevo para no afectar el estado del Owner de otros tests
        var email = $"inactive_{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Inactivo",
            LastName  = "User",
            Email     = email,
            Password  = "Password1!",
            Phone     = "3001234567",
            Role      = "Owner"
        });

        // Desactiva la cuenta vía endpoint de admin
        var adminClient = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var listResp    = await adminClient.GetAsync($"/api/auth/admin/users?search={email}");
        var list        = await listResp.Content.ReadFromJsonAsync<AdminPagedUsers>();
        var userId      = list!.Items.First().Id;

        await adminClient.PatchAsync($"/api/auth/admin/users/{userId}/active?value=false", null);

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email    = email,
            Password = "Password1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResp.StatusCode);
    }

    [Fact]
    public async Task Register_ExistingEmail_AfterSoftDelete_Returns409()
    {
        var email = $"softdel_{Guid.NewGuid():N}@test.com";
        var req   = new RegisterRequest
        {
            FirstName = "ToDelete",
            LastName  = "User",
            Email     = email,
            Password  = "Password1!",
            Phone     = "3001234567",
            Role      = "Owner"
        };

        // Crea el usuario
        var createResp = await _client.PostAsJsonAsync("/api/auth/register", req);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        // Elimínalo vía soft delete de admin
        var adminClient = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var listResp    = await adminClient.GetAsync($"/api/auth/admin/users?search={email}");
        var list        = await listResp.Content.ReadFromJsonAsync<AdminPagedUsers>();
        var userId      = list!.Items.First().Id;
        await adminClient.DeleteAsync($"/api/auth/admin/users/{userId}");

        // Intenta registrar otro usuario con el mismo email — EmailExistsAsync no filtra IsDeleted,
        // por lo que el email sigue siendo único y retorna 409
        var dupResp = await _client.PostAsJsonAsync("/api/auth/register", req);
        Assert.Equal(HttpStatusCode.Conflict, dupResp.StatusCode);
    }

    // ── P2: contraseñas y usuarios eliminados ────────────────────────────────

    [Fact]
    public async Task UpdateMe_AfterChangePassword_NewPasswordWorksForLogin()
    {
        // Crea un usuario propio para no interferir con el admin (que otros tests también resetean)
        var email    = $"pwchange_{Guid.NewGuid():N}@test.com";
        var origPass = "Original1!";
        var newPass  = "Cambiada2@";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "PwChange",
            LastName  = "User",
            Email     = email,
            Password  = origPass,
            Phone     = "3001234567",
            Role      = "Owner"
        });

        // Obtén el ID del usuario recién creado
        var adminClient = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var listResp    = await adminClient.GetAsync($"/api/auth/admin/users?search={email}");
        var list        = await listResp.Content.ReadFromJsonAsync<AdminPagedUsers>();
        var userId      = list!.Items.First().Id;

        // Cambia la contraseña
        var userClient = ClientAs(userId, email, "Owner");
        var changeResp = await userClient.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = origPass,
            NewPassword     = newPass
        });
        Assert.Equal(HttpStatusCode.NoContent, changeResp.StatusCode);

        // Login con la nueva contraseña debe funcionar
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email    = email,
            Password = newPass
        });
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
    }

    [Fact]
    public async Task GetMe_Returns404_WhenUserDeleted()
    {
        // Crea un usuario, obtén su ID y luego elimínalo
        var email = $"deleted_me_{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "ToDelete",
            LastName  = "Me",
            Email     = email,
            Password  = "Password1!",
            Phone     = "3001234567",
            Role      = "Owner"
        });

        var adminClient = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var listResp    = await adminClient.GetAsync($"/api/auth/admin/users?search={email}");
        var list        = await listResp.Content.ReadFromJsonAsync<AdminPagedUsers>();
        var userId      = list!.Items.First().Id;

        await adminClient.DeleteAsync($"/api/auth/admin/users/{userId}");

        // El token del usuario eliminado sigue siendo criptográficamente válido,
        // pero GetByIdAsync filtra IsDeleted=true, por lo que retorna 404
        var deletedUserClient = ClientAs(userId, email, "Owner");
        var resp = await deletedUserClient.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── P3: paginación y validaciones de modelo ──────────────────────────────

    [Fact]
    public async Task AdminListUsers_PageBeyondTotal_ReturnsEmptyItems()
    {
        var client = ClientAs(_factory.AdminId, AuthWebFactory.AdminEmail, "Admin");
        var resp   = await client.GetAsync("/api/auth/admin/users?page=9999&pageSize=15");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<AdminPagedUsers>();
        Assert.NotNull(body);
        Assert.Empty(body.Items);
        Assert.True(body.TotalCount >= 0);
    }

    [Fact]
    public async Task Register_EmptyEmail_Returns400()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Sin",
            LastName  = "Email",
            Email     = "",
            Password  = "Password1!",
            Phone     = "3001234567",
            Role      = "Owner"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Register_EmptyPassword_Returns400()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Sin",
            LastName  = "Password",
            Email     = $"nopass_{Guid.NewGuid():N}@test.com",
            Password  = "",
            Phone     = "3001234567",
            Role      = "Owner"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── P4: token expirado ───────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_ExpiredToken_Returns401()
    {
        // ClockSkew es 30s, así que el token debe haber expirado hace más de 30s para ser rechazado
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtTestHelper.Secret));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   _factory.OwnerId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "owner@test.com"),
            new Claim(ClaimTypes.Name,               "Owner Test"),
            new Claim("phone",                       ""),
            new Claim(ClaimTypes.Role,               "Owner"),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var expiredToken = new JwtSecurityToken(
            issuer:             JwtTestHelper.Issuer,
            audience:           JwtTestHelper.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow.AddSeconds(-100),
            expires:            DateTime.UtcNow.AddSeconds(-61),
            signingCredentials: creds);

        var tokenStr = new JwtSecurityTokenHandler().WriteToken(expiredToken);
        var client   = AuthorizedClient(tokenStr);

        var resp = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
