using AppointmentsService.Application.DTOs;
using AppointmentsService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentsService.API.Controllers;

/// <summary>
/// Gestión de horarios de la clínica y de cada veterinario.
/// Solo accesible para Admin.
/// </summary>
[ApiController]
[Route("api/schedules")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class SchedulesController : ControllerBase
{
    private readonly ScheduleAppService _svc;

    public SchedulesController(ScheduleAppService svc) => _svc = svc;

    // ── ClinicSettings ────────────────────────────────────────────────────────

    /// <summary>Devuelve la configuración global de horario de la clínica.</summary>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(ClinicSettingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings(CancellationToken ct) =>
        Ok(await _svc.GetClinicSettingsAsync(ct));

    /// <summary>Actualiza el horario global de la clínica.</summary>
    [HttpPut("settings")]
    [ProducesResponseType(typeof(ClinicSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateClinicSettingsRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateClinicSettingsAsync(req, ct));

    // ── VeterinarianSchedule ──────────────────────────────────────────────────

    /// <summary>Devuelve el horario personalizado de un veterinario (días que tiene override).</summary>
    [HttpGet("vets/{vetId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<VeterinarianScheduleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVetSchedules(Guid vetId, CancellationToken ct) =>
        Ok(await _svc.GetVetSchedulesAsync(vetId, ct));

    /// <summary>
    /// Crea o reemplaza el horario de un veterinario para un día de la semana.
    /// Si ya existe entrada para ese día, la sobreescribe.
    /// </summary>
    [HttpPut("vets/{vetId:guid}")]
    [ProducesResponseType(typeof(VeterinarianScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertVetSchedule(
        Guid vetId, [FromBody] UpsertVeterinarianScheduleRequest req, CancellationToken ct) =>
        Ok(await _svc.UpsertVetScheduleAsync(vetId, req, ct));

    /// <summary>
    /// Elimina el override de horario de un veterinario para un día.
    /// A partir de ese momento ese día vuelve a usar el horario global.
    /// </summary>
    [HttpDelete("vets/{vetId:guid}/{day}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteVetSchedule(
        Guid vetId, string day, CancellationToken ct)
    {
        await _svc.DeleteVetScheduleAsync(vetId, day, ct);
        return NoContent();
    }

    // ── VeterinarianLeave ─────────────────────────────────────────────────────

    /// <summary>Devuelve todas las ausencias registradas de un veterinario.</summary>
    [HttpGet("leaves/{vetId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<VeterinarianLeaveResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVetLeaves(Guid vetId, CancellationToken ct) =>
        Ok(await _svc.GetVetLeavesAsync(vetId, ct));

    /// <summary>Registra una ausencia para un veterinario.</summary>
    [HttpPost("leaves/{vetId:guid}")]
    [ProducesResponseType(typeof(VeterinarianLeaveResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateVetLeave(
        Guid vetId, [FromBody] CreateVeterinarianLeaveRequest req, CancellationToken ct)
    {
        var result = await _svc.CreateVetLeaveAsync(vetId, req, ct);
        return CreatedAtAction(nameof(GetVetLeaves), new { vetId }, result);
    }

    /// <summary>Elimina una ausencia registrada.</summary>
    [HttpDelete("leaves/{leaveId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteVetLeave(Guid leaveId, CancellationToken ct)
    {
        await _svc.DeleteVetLeaveAsync(leaveId, ct);
        return NoContent();
    }
}
