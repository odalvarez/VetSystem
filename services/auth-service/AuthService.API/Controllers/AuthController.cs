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
        // TODO producción: cambiar Secure = true cuando el servidor opere con HTTPS.
        Response.Cookies.Append("vetsys_jwt", result.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure   = false,
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
    [Authorize(Roles = "Veterinarian")]
    public async Task<IActionResult> GetOwners(CancellationToken ct)
    {
        var result = await _auth.ListOwnersAsync(ct);
        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? throw new UnauthorizedException("Token inválido.");
        return Guid.Parse(sub);
    }
}
