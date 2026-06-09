using System.Security.Claims;
using AppointmentsService.Application.DTOs;
using AppointmentsService.Application.Exceptions;
using AppointmentsService.Application.Services;
using AppointmentsService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentsService.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentAppService _svc;

    public AppointmentsController(AppointmentAppService svc) => _svc = svc;

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAppointmentRequest req, CancellationToken ct)
    {
        var (callerId, isOwner) = GetCallerInfo();
        var result = await _svc.CreateAsync(req, callerId, isOwner, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? veterinarianId = null,
        [FromQuery] Guid? patientId = null,
        CancellationToken ct = default)
    {
        AppointmentStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<AppointmentStatus>(status, ignoreCase: true, out var s))
            parsedStatus = s;

        var (callerId, isOwner) = GetCallerInfo();
        var result = await _svc.ListAsync(callerId, isOwner, parsedStatus, from, to, veterinarianId, patientId, page, pageSize, ct);
        return Ok(result);
    }

    // Availability debe ir ANTES de {id} para evitar que "availability" se interprete como guid
    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] Guid veterinarianId,
        [FromQuery] DateOnly date,
        [FromQuery] int durationMinutes = 30,
        CancellationToken ct = default)
    {
        var result = await _svc.GetAvailabilityAsync(veterinarianId, date, durationMinutes, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var (callerId, isOwner) = GetCallerInfo();
        var result = await _svc.GetAsync(id, callerId, isOwner, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateAppointmentRequest req, CancellationToken ct)
    {
        var (callerId, isOwner) = GetCallerInfo();
        var result = await _svc.UpdateAsync(id, req, callerId, isOwner, ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Veterinarian,Admin")]
    public async Task<IActionResult> ChangeStatus(
        Guid id, [FromBody] ChangeStatusRequest req, CancellationToken ct)
    {
        var result = await _svc.ChangeStatusAsync(id, req.Status, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var (callerId, isOwner) = GetCallerInfo();
        await _svc.CancelAsync(id, callerId, isOwner, ct);
        return NoContent();
    }

    private (Guid Id, bool IsOwner) GetCallerInfo()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedException("Token inválido.");
        return (Guid.Parse(sub), User.IsInRole("Owner"));
    }
}
