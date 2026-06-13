using System.Security.Claims;
using AppointmentsService.Application.DTOs;
using AppointmentsService.Application.Exceptions;
using AppointmentsService.Application.Services;
using AppointmentsService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentsService.API.Controllers;

/// <summary>
/// Gestión de citas y agenda del veterinario.
/// Un <c>Owner</c> solo puede ver y gestionar sus propias citas.
/// Un <c>Veterinarian</c> o <c>Admin</c> puede ver y gestionar todas las citas del sistema.
/// </summary>
[ApiController]
[Route("api/appointments")]
[Authorize]
[Produces("application/json")]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentAppService _svc;

    public AppointmentsController(AppointmentAppService svc) => _svc = svc;

    /// <summary>
    /// Crea una nueva cita. Un <c>Owner</c> solo puede crear citas para sus propias mascotas.
    /// <br/>
    /// <b>Nota:</b> este servicio no consulta patients-service — el caller debe enviar los datos
    /// completos del paciente y el dueño en el request.
    /// </summary>
    /// <param name="req">Datos de la cita: mascota, dueño, veterinario, fecha, duración y motivo.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Cita creada con estado inicial <c>scheduled</c>.</returns>
    /// <response code="201">Cita creada correctamente.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">El Owner intenta crear una cita para una mascota que no le pertenece.</response>
    /// <response code="409">Conflicto de horario: el veterinario ya tiene una cita en ese intervalo.</response>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAppointmentRequest req, CancellationToken ct)
    {
        var (callerId, isOwner) = GetCallerInfo();
        var result = await _svc.CreateAsync(req, callerId, isOwner, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Lista citas con filtros opcionales y paginación.
    /// Un <c>Owner</c> solo recibe sus propias citas independientemente de los filtros enviados.
    /// </summary>
    /// <param name="page">Número de página (default 1).</param>
    /// <param name="pageSize">Registros por página (default 20).</param>
    /// <param name="status">Filtrar por estado: <c>scheduled</c>, <c>confirmed</c>, <c>completed</c>, <c>cancelled</c>, <c>no_show</c>.</param>
    /// <param name="from">Fecha y hora de inicio del rango (ISO 8601).</param>
    /// <param name="to">Fecha y hora de fin del rango (ISO 8601).</param>
    /// <param name="veterinarianId">Filtrar por veterinario (solo Veterinarian/Admin).</param>
    /// <param name="patientId">Filtrar por mascota.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Página de citas con conteo total.</returns>
    /// <response code="200">Lista de citas devuelta correctamente.</response>
    /// <response code="401">No autenticado.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Consulta los intervalos de tiempo disponibles de un veterinario en una fecha específica.
    /// Devuelve los slots libres según las citas ya agendadas y el horario laboral configurado.
    /// </summary>
    /// <param name="veterinarianId">ID (GUID) del veterinario.</param>
    /// <param name="date">Fecha a consultar en formato <c>YYYY-MM-DD</c>.</param>
    /// <param name="durationMinutes">Duración deseada para la cita en minutos (default 30).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista de slots disponibles con hora de inicio y fin.</returns>
    /// <response code="200">Disponibilidad calculada correctamente.</response>
    /// <response code="401">No autenticado.</response>
    // Availability debe ir ANTES de {id} para evitar que "availability" se interprete como guid
    [HttpGet("availability")]
    [ProducesResponseType(typeof(AvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] Guid veterinarianId,
        [FromQuery] DateOnly date,
        [FromQuery] int durationMinutes = 30,
        CancellationToken ct = default)
    {
        var result = await _svc.GetAvailabilityAsync(veterinarianId, date, durationMinutes, ct);
        return Ok(result);
    }

    /// <summary>Obtiene una cita por su ID.</summary>
    /// <param name="id">ID (GUID) de la cita.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Datos completos de la cita incluyendo paciente, dueño, veterinario y estado.</returns>
    /// <response code="200">Cita encontrada.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">El Owner intenta ver una cita que no le pertenece.</response>
    /// <response code="404">Cita no encontrada.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var (callerId, isOwner) = GetCallerInfo();
        var result = await _svc.GetAsync(id, callerId, isOwner, ct);
        return Ok(result);
    }

    /// <summary>
    /// Actualiza la fecha, duración, motivo y notas de una cita.
    /// Solo pueden editarse citas en estado <c>scheduled</c>.
    /// </summary>
    /// <param name="id">ID (GUID) de la cita.</param>
    /// <param name="req">Nuevos datos de la cita.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Cita con los datos actualizados.</returns>
    /// <response code="200">Cita actualizada correctamente.</response>
    /// <response code="400">Datos inválidos o la cita no está en estado <c>scheduled</c>.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Sin permiso sobre esta cita.</response>
    /// <response code="404">Cita no encontrada.</response>
    /// <response code="409">Conflicto de horario con otra cita del mismo veterinario.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateAppointmentRequest req, CancellationToken ct)
    {
        var (callerId, isOwner) = GetCallerInfo();
        var result = await _svc.UpdateAsync(id, req, callerId, isOwner, ct);
        return Ok(result);
    }

    /// <summary>
    /// Cambia el estado de una cita. Solo disponible para Veterinarian y Admin.
    /// Transiciones válidas: <c>scheduled → confirmed → completed</c> o <c>* → cancelled / no_show</c>.
    /// </summary>
    /// <param name="id">ID (GUID) de la cita.</param>
    /// <param name="req">Nuevo estado de la cita.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Cita con el nuevo estado.</returns>
    /// <response code="200">Estado actualizado correctamente.</response>
    /// <response code="400">Transición de estado inválida.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Veterinarian y Admin).</response>
    /// <response code="404">Cita no encontrada.</response>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Veterinarian,Admin")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(
        Guid id, [FromBody] ChangeStatusRequest req, CancellationToken ct)
    {
        var result = await _svc.ChangeStatusAsync(id, req.Status, ct);
        return Ok(result);
    }

    /// <summary>
    /// Cancela (lógicamente) una cita. No se elimina de la base de datos.
    /// Un <c>Owner</c> solo puede cancelar sus propias citas y únicamente si están en estado <c>scheduled</c>.
    /// </summary>
    /// <param name="id">ID (GUID) de la cita a cancelar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="204">Cita cancelada correctamente.</response>
    /// <response code="400">La cita no puede cancelarse en su estado actual.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Sin permiso para cancelar esta cita.</response>
    /// <response code="404">Cita no encontrada.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
