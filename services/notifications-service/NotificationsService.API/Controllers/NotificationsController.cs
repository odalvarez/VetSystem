using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationsService.Application.DTOs;
using NotificationsService.Application.Services;

namespace NotificationsService.API.Controllers;

/// <summary>
/// Envío de notificaciones y programación de recordatorios de citas.
/// Soporta dos canales: <b>WhatsApp</b> (via Evolution API) y <b>correo electrónico</b> (via SMTP).
/// <br/>
/// <b>Nota de seguridad:</b> este servicio requiere el header <c>X-Internal-Key</c> en producción.
/// Nginx lo inyecta automáticamente — el navegador nunca lo envía directamente.
/// En Swagger, las llamadas pasan sin la clave interna porque la ruta <c>/swagger</c> está exenta.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationAppService _svc;

    public NotificationsController(NotificationAppService svc) => _svc = svc;

    /// <summary>
    /// Envía un mensaje de WhatsApp a un número de teléfono vía Evolution API.
    /// La notificación se encola y se procesa de forma asíncrona.
    /// </summary>
    /// <param name="req">Número de destino con código de país (ej: <c>573001234567</c>) y texto del mensaje (máx. 4096 chars).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>ID y estado inicial de la notificación (<c>pending</c>).</returns>
    /// <response code="202">Notificación encolada. El envío se procesa en segundo plano.</response>
    /// <response code="400">Número de teléfono inválido o mensaje vacío/demasiado largo.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="503">Evolution API no disponible.</response>
    [HttpPost("whatsapp")]
    [ProducesResponseType(typeof(NotificationAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SendWhatsApp(
        [FromBody] SendWhatsAppRequest req, CancellationToken ct)
    {
        var result = await _svc.SendWhatsAppAsync(req, ct);
        return Accepted(result);
    }

    /// <summary>
    /// Envía un correo electrónico vía SMTP.
    /// El cuerpo acepta HTML para mensajes con formato.
    /// La notificación se encola y se procesa de forma asíncrona.
    /// </summary>
    /// <param name="req">Email de destino, asunto (máx. 200 chars) y cuerpo del mensaje (HTML permitido).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>ID y estado inicial de la notificación (<c>pending</c>).</returns>
    /// <response code="202">Correo encolado. El envío se procesa en segundo plano.</response>
    /// <response code="400">Email inválido o asunto vacío/demasiado largo.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="503">Servidor SMTP no disponible.</response>
    [HttpPost("email")]
    [ProducesResponseType(typeof(NotificationAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SendEmail(
        [FromBody] SendEmailRequest req, CancellationToken ct)
    {
        var result = await _svc.SendEmailAsync(req, ct);
        return Accepted(result);
    }

    /// <summary>
    /// Programa un recordatorio automático de cita.
    /// El sistema calcula el momento de envío como 24 horas antes de <c>scheduledAt</c>
    /// y lo despacha por los canales indicados (WhatsApp, email o ambos).
    /// </summary>
    /// <param name="req">Datos de la cita y canales de notificación deseados.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>ID del recordatorio y fecha/hora programada de envío.</returns>
    /// <response code="202">Recordatorio programado correctamente.</response>
    /// <response code="400">Datos inválidos o <c>scheduledAt</c> está en el pasado.</response>
    /// <response code="401">No autenticado.</response>
    [HttpPost("reminder")]
    [ProducesResponseType(typeof(ReminderAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ScheduleReminder(
        [FromBody] ScheduleReminderRequest req, CancellationToken ct)
    {
        var result = await _svc.ScheduleReminderAsync(req, ct);
        return Accepted(result);
    }

    /// <summary>
    /// Lista el historial de notificaciones con paginación.
    /// <list type="bullet">
    /// <item><description><b>Owner:</b> solo ve las notificaciones enviadas a su email o teléfono (pasa <c>phone</c> como query param para incluir las de WhatsApp).</description></item>
    /// <item><description><b>Veterinarian / Admin:</b> ve todas las notificaciones del sistema.</description></item>
    /// </list>
    /// </summary>
    /// <param name="page">Número de página (default 1).</param>
    /// <param name="pageSize">Registros por página (default 50).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de notificaciones con estado, tipo de canal y timestamps.</returns>
    /// <response code="200">Lista devuelta correctamente.</response>
    /// <response code="401">No autenticado.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListAll(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        pageSize = Math.Min(pageSize, 200);

        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        IReadOnlyList<string>? recipientFilter = null;

        // El propietario solo ve las notificaciones enviadas a su correo o su teléfono
        if (role.Equals("Owner", StringComparison.OrdinalIgnoreCase))
        {
            var email    = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
            var phone    = User.FindFirst("phone")?.Value ?? "";
            var contacts = new List<string>();
            if (!string.IsNullOrWhiteSpace(email)) contacts.Add(email);
            if (!string.IsNullOrWhiteSpace(phone)) contacts.Add(phone);
            recipientFilter = contacts;
        }

        var result = await _svc.ListAllAsync(page, pageSize, recipientFilter, ct);
        return Ok(result);
    }

    /// <summary>Consulta el estado actual de una notificación específica.</summary>
    /// <param name="id">ID (GUID) de la notificación.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Estado de la notificación: <c>pending</c>, <c>sent</c> o <c>failed</c>, con detalle del error si aplica.</returns>
    /// <response code="200">Notificación encontrada.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="404">Notificación no encontrada.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken ct)
    {
        var result = await _svc.GetStatusAsync(id, ct);
        return Ok(result);
    }
}
