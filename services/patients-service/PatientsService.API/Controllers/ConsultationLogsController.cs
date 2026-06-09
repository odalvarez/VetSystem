using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientsService.Application.DTOs;
using PatientsService.Application.Services;

namespace PatientsService.API.Controllers;

[ApiController]
[Route("api/patients/{patientId:guid}/logs")]
[Authorize]
public class ConsultationLogsController : ControllerBase
{
    private readonly ConsultationLogAppService _svc;

    public ConsultationLogsController(ConsultationLogAppService svc) => _svc = svc;

    [HttpPost]
    [Authorize(Roles = "Veterinarian,Admin")]
    public async Task<IActionResult> Create(
        Guid patientId, [FromBody] CreateConsultationLogRequest req, CancellationToken ct)
    {
        var (vetId, vetName) = GetCaller();
        var result = await _svc.CreateAsync(patientId, req, vetId, vetName, ct);
        return CreatedAtAction(nameof(GetById), new { patientId, logId = result.Id }, result);
    }

    [HttpGet]
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

    [HttpGet("{logId:guid}")]
    public async Task<IActionResult> GetById(Guid patientId, Guid logId, CancellationToken ct)
    {
        Guid? ownerFilter = IsOwner() ? GetCallerId() : null;
        var result = await _svc.GetAsync(patientId, logId, ownerFilter, ct);
        return Ok(result);
    }

    [HttpPut("{logId:guid}")]
    [Authorize(Roles = "Veterinarian,Admin")]
    public async Task<IActionResult> Update(
        Guid patientId, Guid logId,
        [FromBody] UpdateConsultationLogRequest req, CancellationToken ct)
    {
        var result = await _svc.UpdateAsync(patientId, logId, req, ct);
        return Ok(result);
    }

    [HttpPatch("{logId:guid}/close")]
    [Authorize(Roles = "Veterinarian,Admin")]
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
