using System.Security.Claims;
using AuthService.Application.DTOs;
using AuthService.Application.Exceptions;
using AuthService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthService.API.Controllers;

/// <summary>
/// Registro de usuarios, autenticación y gestión de roles.
/// El JWT viaja en la cookie httpOnly <c>vetsys_jwt</c> — el cuerpo de login nunca expone el token.
/// Para probar desde Swagger UI: haz login, copia el token del campo <c>accessToken</c>
/// de la respuesta interna y pégalo en el botón <b>Authorize</b>.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AuthApplicationService _auth;

    public AuthController(AuthApplicationService auth) => _auth = auth;

    /// <summary>Registra un nuevo usuario en el sistema.</summary>
    /// <param name="req">Datos del nuevo usuario. El rol aceptado es <c>Veterinarian</c> o <c>Owner</c>.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Confirmación con el ID, email, rol y fecha de creación.</returns>
    /// <response code="201">Usuario creado correctamente.</response>
    /// <response code="400">Datos inválidos o incompletos (ej. contraseña menor de 8 caracteres).</response>
    /// <response code="409">El correo ya está registrado.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest req, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(req, ct);
        return CreatedAtAction(nameof(GetMe), null, result);
    }

    /// <summary>
    /// Autentica al usuario. El JWT se establece como cookie httpOnly <c>vetsys_jwt</c>.
    /// El cuerpo de respuesta contiene solo los datos del usuario — el token nunca se expone.
    /// </summary>
    /// <param name="req">Credenciales de acceso.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Datos del usuario autenticado. La cookie <c>vetsys_jwt</c> queda establecida en el navegador.</returns>
    /// <response code="200">Autenticación exitosa. Cookie establecida.</response>
    /// <response code="400">Cuerpo de request inválido.</response>
    /// <response code="401">Credenciales incorrectas.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(LoginUserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        return Ok(new { user = result.User });
    }

    /// <summary>Cierra la sesión eliminando la cookie <c>vetsys_jwt</c> del navegador.</summary>
    /// <response code="204">Cookie eliminada correctamente.</response>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        // Las opciones deben coincidir exactamente con las del Append para que el browser borre la cookie
        Response.Cookies.Delete("vetsys_jwt", new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Path     = "/"
        });
        return NoContent();
    }

    /// <summary>Devuelve el perfil completo del usuario autenticado.</summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Perfil del usuario: ID, nombre, email, teléfono, rol y fecha de creación.</returns>
    /// <response code="200">Perfil devuelto correctamente.</response>
    /// <response code="401">Cookie ausente o inválida.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _auth.GetProfileAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Actualiza el nombre y teléfono del usuario autenticado.
    /// El email y el rol no son modificables por este endpoint.
    /// </summary>
    /// <param name="req">Nuevos datos de perfil.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Perfil actualizado.</returns>
    /// <response code="200">Perfil actualizado correctamente.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="401">No autenticado.</response>
    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateProfileRequest req, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _auth.UpdateProfileAsync(userId, req, ct);
        return Ok(result);
    }

    /// <summary>Cambia la contraseña del usuario autenticado. Requiere la contraseña actual.</summary>
    /// <param name="req">Contraseña actual y nueva contraseña (mín. 8 caracteres).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="204">Contraseña cambiada correctamente.</response>
    /// <response code="400">Nueva contraseña inválida o no cumple los requisitos mínimos.</response>
    /// <response code="401">Token inválido o contraseña actual incorrecta.</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        await _auth.ChangePasswordAsync(userId, req, ct);
        return NoContent();
    }

    /// <summary>
    /// Lista todos los propietarios registrados en el sistema.
    /// Usado principalmente para poblar selects al registrar mascotas o citas.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de propietarios con ID, nombre completo, email y teléfono.</returns>
    /// <response code="200">Lista devuelta correctamente.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Veterinarian y Admin).</response>
    [HttpGet("owners")]
    [Authorize(Roles = "Veterinarian,Admin")]
    [ProducesResponseType(typeof(IEnumerable<OwnerSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOwners(CancellationToken ct)
    {
        var result = await _auth.ListOwnersAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Lista todos los veterinarios registrados.
    /// Usado para poblar selects al agendar citas. Disponible para todos los roles autenticados.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de veterinarios con ID, nombre completo, email y teléfono.</returns>
    /// <response code="200">Lista devuelta correctamente.</response>
    /// <response code="401">No autenticado.</response>
    [HttpGet("veterinarians")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<OwnerSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetVeterinarians(CancellationToken ct)
    {
        var result = await _auth.ListVeterinariansAsync(ct);
        return Ok(result);
    }

    // ── Admin endpoints ───────────────────────────────────────────────────────────

    /// <summary>
    /// Lista todos los usuarios del sistema con soporte de filtros y paginación.
    /// Solo accesible por administradores.
    /// </summary>
    /// <param name="role">Filtrar por rol: <c>Admin</c>, <c>Veterinarian</c> o <c>Owner</c>.</param>
    /// <param name="search">Búsqueda parcial por nombre o email.</param>
    /// <param name="isActive">Filtrar por estado activo (<c>true</c>) o inactivo (<c>false</c>).</param>
    /// <param name="page">Número de página (default 1).</param>
    /// <param name="pageSize">Registros por página (default 15).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Página de usuarios con conteo total.</returns>
    /// <response code="200">Lista paginada de usuarios.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Admin).</response>
    [HttpGet("admin/users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(AdminPagedUsers), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

    /// <summary>Obtiene los datos completos de un usuario específico por su ID.</summary>
    /// <param name="id">ID (GUID) del usuario.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Datos completos del usuario incluyendo estado activo/inactivo.</returns>
    /// <response code="200">Usuario encontrado.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Admin).</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpGet("admin/users/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(AdminUserItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminGetUser(Guid id, CancellationToken ct)
    {
        var result = await _auth.AdminGetUserAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Crea un usuario nuevo desde el panel de administración.
    /// Permite asignar cualquier rol incluyendo <c>Admin</c>.
    /// </summary>
    /// <param name="req">Datos del nuevo usuario.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Usuario creado con su ID asignado.</returns>
    /// <response code="201">Usuario creado correctamente.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Admin).</response>
    /// <response code="409">El correo ya está registrado.</response>
    [HttpPost("admin/users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(AdminUserItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AdminCreateUser(
        [FromBody] AdminCreateUserRequest req, CancellationToken ct)
    {
        var result = await _auth.AdminCreateUserAsync(req, ct);
        return CreatedAtAction(nameof(AdminGetUser), new { id = result.Id }, result);
    }

    /// <summary>Actualiza nombre, teléfono y rol de un usuario existente.</summary>
    /// <param name="id">ID (GUID) del usuario a actualizar.</param>
    /// <param name="req">Nuevos datos del usuario.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Usuario con los datos actualizados.</returns>
    /// <response code="200">Usuario actualizado.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Admin).</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpPut("admin/users/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(AdminUserItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminUpdateUser(
        Guid id, [FromBody] AdminUpdateUserRequest req, CancellationToken ct)
    {
        var result = await _auth.AdminUpdateUserAsync(id, req, ct);
        return Ok(result);
    }

    /// <summary>
    /// Activa o desactiva una cuenta de usuario.
    /// Un administrador no puede desactivar su propia cuenta.
    /// </summary>
    /// <param name="id">ID (GUID) del usuario a activar o desactivar.</param>
    /// <param name="value"><c>true</c> para activar, <c>false</c> para desactivar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="204">Estado actualizado correctamente.</response>
    /// <response code="400">El administrador intentó desactivar su propia cuenta.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Admin).</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpPatch("admin/users/{id:guid}/active")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminSetActive(
        Guid id, [FromQuery] bool value, CancellationToken ct)
    {
        if (id == GetCurrentUserId())
            return BadRequest(new { detail = "No puedes desactivar tu propia cuenta." });

        await _auth.AdminSetActiveAsync(id, value, ct);
        return NoContent();
    }

    /// <summary>
    /// Restablece la contraseña de un usuario sin requerir la contraseña actual.
    /// Operación exclusiva del administrador para soporte a usuarios bloqueados.
    /// </summary>
    /// <param name="id">ID (GUID) del usuario.</param>
    /// <param name="req">Nueva contraseña (mín. 8 caracteres).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="204">Contraseña restablecida correctamente.</response>
    /// <response code="400">Nueva contraseña inválida.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Admin).</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpPost("admin/users/{id:guid}/reset-password")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminResetPassword(
        Guid id, [FromBody] AdminResetPasswordRequest req, CancellationToken ct)
    {
        await _auth.AdminResetPasswordAsync(id, req.NewPassword, ct);
        return NoContent();
    }

    /// <summary>Elimina (soft delete) un usuario. Solo Admin.</summary>
    /// <response code="204">Usuario eliminado.</response>
    /// <response code="400">El administrador intentó eliminarse a sí mismo.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Admin).</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpDelete("admin/users/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminDeleteUser(Guid id, CancellationToken ct)
    {
        await _auth.AdminDeleteUserAsync(id, GetCurrentUserId(), ct);
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
