using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientsService.Application.DTOs;
using PatientsService.Application.Services;

namespace PatientsService.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[Produces("application/json")]
public class VaccinationsController : ControllerBase
{
    private readonly VaccinationAppService _svc;
    public VaccinationsController(VaccinationAppService svc) => _svc = svc;

    // ── Catálogo ──────────────────────────────────────────────────────────────

    /// <summary>Lista todas las vacunas definidas en el sistema.</summary>
    [HttpGet("vaccines")]
    [ProducesResponseType(typeof(IReadOnlyList<VaccineDefinitionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListDefinitions(CancellationToken ct) =>
        Ok(await _svc.ListDefinitionsAsync(ct));

    /// <summary>Crea una nueva vacuna en el catálogo.</summary>
    [HttpPost("vaccines")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VaccineDefinitionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDefinition([FromBody] CreateVaccineDefinitionRequest req, CancellationToken ct)
    {
        var result = await _svc.CreateDefinitionAsync(req, ct);
        return CreatedAtAction(nameof(ListDefinitions), new { }, result);
    }

    /// <summary>Actualiza nombre, descripción e intervalo de una vacuna.</summary>
    [HttpPut("vaccines/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VaccineDefinitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDefinition(Guid id, [FromBody] UpdateVaccineDefinitionRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateDefinitionAsync(id, req, ct));

    /// <summary>Activa o desactiva una vacuna del catálogo.</summary>
    [HttpPatch("vaccines/{id:guid}/active")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActive(Guid id, [FromQuery] bool value, CancellationToken ct)
    {
        await _svc.ToggleActiveAsync(id, value, ct);
        return NoContent();
    }

    // ── Registros por paciente ─────────────────────────────────────────────────

    /// <summary>Lista el historial de vacunación de una mascota.</summary>
    [HttpGet("patients/{patientId:guid}/vaccinations")]
    [ProducesResponseType(typeof(IReadOnlyList<VaccinationRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(Guid patientId, CancellationToken ct)
    {
        var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role     = User.FindFirstValue(ClaimTypes.Role) ?? "";
        return Ok(await _svc.ListByPatientAsync(patientId, callerId, role, ct));
    }

    /// <summary>
    /// Registra una dosis aplicada. El sistema calcula el número de dosis y la próxima fecha
    /// basándose en el esquema de la vacuna. El vet puede sobrescribir la fecha propuesta.
    /// </summary>
    [HttpPost("patients/{patientId:guid}/vaccinations")]
    [Authorize(Roles = "Veterinarian,Admin")]
    [ProducesResponseType(typeof(VaccinationRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Register(Guid patientId, [FromBody] RegisterVaccinationRequest req, CancellationToken ct)
    {
        var vetId   = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var vetName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? "Veterinario";
        var result  = await _svc.RegisterAsync(patientId, req, vetId, vetName, ct);
        return CreatedAtAction(nameof(List), new { patientId }, result);
    }

    /// <summary>Elimina un registro de vacunación (solo Admin).</summary>
    [HttpDelete("patients/{patientId:guid}/vaccinations/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid patientId, Guid id, CancellationToken ct)
    {
        await _svc.DeleteRecordAsync(id, ct);
        return NoContent();
    }
}
