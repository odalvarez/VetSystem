using System.Security.Claims;
using AuthService.Application.DTOs;
using AuthService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthApplicationService _auth;

    public AuthController(AuthApplicationService auth) => _auth = auth;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest req, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(req, ct);
        return CreatedAtAction(nameof(GetMe), null, result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest req, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(req, ct);

        // El JWT nunca llega al navegador como dato legible: vive solo en la cookie httpOnly.
        // JS no puede acceder a ella, por lo que XSS no puede robar el token.
        Response.Cookies.Append("vetsys_jwt", result.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.AddSeconds(result.ExpiresIn),
            Path     = "/"
        });

        // Solo retornamos la info del usuario; el token ya quedó en la cookie
        return Ok(new { user = result.User });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        // Borramos la cookie con las mismas opciones con las que fue creada
        Response.Cookies.Delete("vetsys_jwt", new CookieOptions { Path = "/" });
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _auth.GetProfileAsync(userId, ct);
        return Ok(result);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateProfileRequest req, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _auth.UpdateProfileAsync(userId, req, ct);
        return Ok(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        await _auth.ChangePasswordAsync(userId, req, ct);
        return NoContent();
    }

    [HttpGet("owners")]
    [Authorize(Roles = "Veterinarian,Admin")]
    public async Task<IActionResult> GetOwners(CancellationToken ct)
    {
        var result = await _auth.ListOwnersAsync(ct);
        return Ok(result);
    }

    // Cualquier usuario autenticado puede ver la lista de vets para agendar citas
    [HttpGet("veterinarians")]
    [Authorize]
    public async Task<IActionResult> GetVeterinarians(CancellationToken ct)
    {
        var result = await _auth.ListVeterinariansAsync(ct);
        return Ok(result);
    }

    // ── Admin endpoints ───────────────────────────────────────────────────────────

    [HttpGet("admin/users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminListUsers(
        [FromQuery] string? role     = null,
        [FromQuery] string? search   = null,
        [FromQuery] bool?   isActive = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 15,
        CancellationToken ct = default)
    {
        var result = await _auth.AdminListUsersAsync(role, search, isActive, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("admin/users/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminGetUser(Guid id, CancellationToken ct)
    {
        var result = await _auth.AdminGetUserAsync(id, ct);
        return Ok(result);
    }

    [HttpPost("admin/users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminCreateUser(
        [FromBody] AdminCreateUserRequest req, CancellationToken ct)
    {
        var result = await _auth.AdminCreateUserAsync(req, ct);
        return CreatedAtAction(nameof(AdminGetUser), new { id = result.Id }, result);
    }

    [HttpPut("admin/users/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminUpdateUser(
        Guid id, [FromBody] AdminUpdateUserRequest req, CancellationToken ct)
    {
        var result = await _auth.AdminUpdateUserAsync(id, req, ct);
        return Ok(result);
    }

    [HttpPatch("admin/users/{id:guid}/active")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminSetActive(
        Guid id, [FromQuery] bool value, CancellationToken ct)
    {
        // El admin no puede desactivarse a sí mismo
        if (id == GetCurrentUserId())
            return BadRequest(new { detail = "No puedes desactivar tu propia cuenta." });

        await _auth.AdminSetActiveAsync(id, value, ct);
        return NoContent();
    }

    [HttpPost("admin/users/{id:guid}/reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminResetPassword(
        Guid id, [FromBody] AdminResetPasswordRequest req, CancellationToken ct)
    {
        await _auth.AdminResetPasswordAsync(id, req.NewPassword, ct);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? throw new UnauthorizedException("Token inválido.");
        return Guid.Parse(sub);
    }
}
