using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientsService.Application.DTOs;
using PatientsService.Application.Services;

namespace PatientsService.API.Controllers;

/// <summary>
/// Bitácoras de consulta — registro en tiempo real de lo ocurrido durante una consulta veterinaria.
/// Ciclo de vida: <c>Open</c> → <c>Closed</c> (irreversible). Una bitácora cerrada no puede reabrirse ni modificarse.
/// Se diferencia de la historia clínica en que captura el detalle clínico durante la atención,
/// mientras que la historia clínica es el resumen médico oficial redactado al finalizar.
/// </summary>
[ApiController]
[Route("api/patients/{patientId:guid}/logs")]
[Authorize]
[Produces("application/json")]
public class ConsultationLogsController : ControllerBase
{
    private readonly ConsultationLogAppService _svc;

    public ConsultationLogsController(ConsultationLogAppService svc) => _svc = svc;

    /// <summary>
    /// Abre una nueva bitácora de consulta para una mascota.
    /// Solo disponible para Veterinarian y Admin. El campo <c>reasonForVisit</c> es obligatorio.
    /// </summary>
    /// <param name="patientId">ID (GUID) de la mascota.</param>
    /// <param name="req">Datos iniciales de la consulta: motivo de visita, signos vitales, anamnesis, etc.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Bitácora creada con estado <c>Open</c> y el nombre del veterinario que la abrió.</returns>
    /// <response code="201">Bitácora creada con estado <c>Open</c>.</response>
    /// <response code="400">Datos inválidos (<c>reasonForVisit</c> es requerido, máx. 500 caracteres).</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Veterinarian y Admin).</response>
    /// <response code="404">Mascota no encontrada.</response>
    [HttpPost]
    [Authorize(Roles = "Veterinarian,Admin")]
    [ProducesResponseType(typeof(ConsultationLogResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        Guid patientId, [FromBody] CreateConsultationLogRequest req, CancellationToken ct)
    {
        var (vetId, vetName) = GetCaller();
        var result = await _svc.CreateAsync(patientId, req, vetId, vetName, ct);
        return CreatedAtAction(nameof(GetById), new { patientId, logId = result.Id }, result);
    }

    /// <summary>Devuelve la bitácora asociada a una cita específica, o 404 si no existe.</summary>
    [HttpGet("by-appointment/{appointmentId:guid}")]
    [ProducesResponseType(typeof(ConsultationLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAppointment(Guid patientId, Guid appointmentId, CancellationToken ct)
    {
        Guid? ownerFilter = IsOwner() ? GetCallerId() : null;
        var result = await _svc.GetByAppointmentAsync(patientId, appointmentId, ownerFilter, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Lista las bitácoras de consulta de una mascota, del más reciente al más antiguo.
    /// Un <c>Owner</c> solo puede consultar las bitácoras de sus propias mascotas.
    /// </summary>
    /// <param name="patientId">ID (GUID) de la mascota.</param>
    /// <param name="page">Número de página (default 1).</param>
    /// <param name="pageSize">Registros por página (default 20).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Página de bitácoras con estado, signos vitales, diagnóstico y datos del veterinario.</returns>
    /// <response code="200">Lista de bitácoras devuelta correctamente.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">El Owner intenta acceder a una mascota que no le pertenece.</response>
    /// <response code="404">Mascota no encontrada.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ConsultationLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        Guid patientId,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        Guid? ownerFilter = IsOwner() ? GetCallerId() : null;
        var result = await _svc.ListAsync(patientId, ownerFilter, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Obtiene una bitácora de consulta específica por su ID.</summary>
    /// <param name="patientId">ID (GUID) de la mascota.</param>
    /// <param name="logId">ID (GUID) de la bitácora.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Bitácora completa con todos sus campos clínicos y estado actual.</returns>
    /// <response code="200">Bitácora encontrada.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Sin permiso sobre esta mascota.</response>
    /// <response code="404">Mascota o bitácora no encontrada.</response>
    [HttpGet("{logId:guid}")]
    [ProducesResponseType(typeof(ConsultationLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid patientId, Guid logId, CancellationToken ct)
    {
        Guid? ownerFilter = IsOwner() ? GetCallerId() : null;
        var result = await _svc.GetAsync(patientId, logId, ownerFilter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Actualiza los campos de una bitácora de consulta abierta.
    /// Solo puede modificarse si <c>status == "Open"</c> — una bitácora cerrada es inmutable.
    /// Solo disponible para Veterinarian y Admin.
    /// </summary>
    /// <param name="patientId">ID (GUID) de la mascota.</param>
    /// <param name="logId">ID (GUID) de la bitácora.</param>
    /// <param name="req">Campos actualizados de la bitácora (mismo esquema que POST).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Bitácora con los datos actualizados.</returns>
    /// <response code="200">Bitácora actualizada correctamente.</response>
    /// <response code="400">La bitácora ya está cerrada y no puede modificarse.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Veterinarian y Admin).</response>
    /// <response code="404">Mascota o bitácora no encontrada.</response>
    [HttpPut("{logId:guid}")]
    [Authorize(Roles = "Veterinarian,Admin")]
    [ProducesResponseType(typeof(ConsultationLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid patientId, Guid logId,
        [FromBody] UpdateConsultationLogRequest req, CancellationToken ct)
    {
        var result = await _svc.UpdateAsync(patientId, logId, req, ct);
        return Ok(result);
    }

    /// <summary>
    /// Cierra una bitácora de consulta. Operación <b>irreversible</b>.
    /// Una vez cerrada, la bitácora no puede reabrirse ni modificarse.
    /// Se registra automáticamente la fecha y hora del cierre (<c>closedAt</c>).
    /// Solo disponible para Veterinarian y Admin.
    /// </summary>
    /// <param name="patientId">ID (GUID) de la mascota.</param>
    /// <param name="logId">ID (GUID) de la bitácora a cerrar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Bitácora con estado <c>Closed</c> y <c>closedAt</c> establecido.</returns>
    /// <response code="200">Bitácora cerrada exitosamente.</response>
    /// <response code="400">La bitácora ya estaba cerrada.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Veterinarian y Admin).</response>
    /// <response code="404">Mascota o bitácora no encontrada.</response>
    [HttpPatch("{logId:guid}/close")]
    [Authorize(Roles = "Veterinarian,Admin")]
    [ProducesResponseType(typeof(ConsultationLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(Guid patientId, Guid logId, CancellationToken ct)
    {
        var result = await _svc.CloseAsync(patientId, logId, ct);
        return Ok(result);
    }

    private bool IsOwner() => User.IsInRole("Owner");

    private Guid GetCallerId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? throw new UnauthorizedAccessException("Token inválido.");
        return Guid.Parse(sub);
    }

    private (Guid Id, string Name) GetCaller()
    {
        var sub  = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Token inválido.");
        var name = User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("email")
                ?? "Desconocido";
        return (Guid.Parse(sub), name);
    }
}
