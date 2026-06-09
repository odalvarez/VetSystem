using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationsService.Application.DTOs;
using NotificationsService.Application.Services;

namespace NotificationsService.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationAppService _svc;

    public NotificationsController(NotificationAppService svc) => _svc = svc;

    [HttpPost("whatsapp")]
    public async Task<IActionResult> SendWhatsApp(
        [FromBody] SendWhatsAppRequest req, CancellationToken ct)
    {
        var result = await _svc.SendWhatsAppAsync(req, ct);
        return Accepted(result);
    }

    [HttpPost("email")]
    public async Task<IActionResult> SendEmail(
        [FromBody] SendEmailRequest req, CancellationToken ct)
    {
        var result = await _svc.SendEmailAsync(req, ct);
        return Accepted(result);
    }

    [HttpPost("reminder")]
    public async Task<IActionResult> ScheduleReminder(
        [FromBody] ScheduleReminderRequest req, CancellationToken ct)
    {
        var result = await _svc.ScheduleReminderAsync(req, ct);
        return Accepted(result);
    }

    [HttpGet]
    public async Task<IActionResult> ListAll(
        [FromQuery] int    page     = 1,
        [FromQuery] int    pageSize = 50,
        [FromQuery] string? phone   = null,
        CancellationToken ct = default)
    {
        var role  = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        IReadOnlyList<string>? recipientFilter = null;

        // El propietario solo ve las notificaciones enviadas a su correo o su teléfono
        if (role.Equals("Owner", StringComparison.OrdinalIgnoreCase))
        {
            var email    = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
            var contacts = new List<string>();
            if (!string.IsNullOrWhiteSpace(email)) contacts.Add(email);
            if (!string.IsNullOrWhiteSpace(phone))  contacts.Add(phone);
            recipientFilter = contacts;
        }

        var result = await _svc.ListAllAsync(page, pageSize, recipientFilter, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken ct)
    {
        var result = await _svc.GetStatusAsync(id, ct);
        return Ok(result);
    }
}
