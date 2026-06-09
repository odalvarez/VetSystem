using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientsService.Application.DTOs;
using PatientsService.Application.Services;

namespace PatientsService.API.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly PatientAppService _svc;

    public PatientsController(PatientAppService svc) => _svc = svc;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest req, CancellationToken ct)
    {
        Guid   ownerId;
        string ownerName;
        string ownerPhone;

        if (IsOwner())
        {
            // El owner crea su propia mascota; los datos del dueño vienen del JWT
            var caller = GetCaller();
            ownerId    = caller.Id;
            ownerName  = caller.Name;
            ownerPhone = caller.Phone;
        }
        else
        {
            // El veterinario debe proveer el OwnerId del dueño en el request
            if (req.OwnerId is null || string.IsNullOrWhiteSpace(req.OwnerName))
                return BadRequest(new { detail = "El veterinario debe indicar OwnerId y OwnerName al registrar una mascota." });

            ownerId    = req.OwnerId.Value;
            ownerName  = req.OwnerName;
            ownerPhone = req.OwnerPhone ?? "";
        }

        var result = await _svc.CreateAsync(req, ownerId, ownerName, ownerPhone, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? species = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        // owner solo ve las suyas; veterinarian ve todas
        Guid? ownerFilter = IsOwner() ? GetCaller().Id : null;
        var result = await _svc.ListAsync(ownerFilter, species, search, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        Guid? ownerFilter = IsOwner() ? GetCaller().Id : null;
        var result = await _svc.GetAsync(id, ownerFilter, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdatePatientRequest req, CancellationToken ct)
    {
        Guid? ownerFilter = IsOwner() ? GetCaller().Id : null;
        var result = await _svc.UpdateAsync(id, req, ownerFilter, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Veterinarian")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/records")]
    [Authorize(Roles = "Veterinarian")]
    public async Task<IActionResult> AddRecord(
        Guid id, [FromBody] CreateClinicalRecordRequest req, CancellationToken ct)
    {
        var (vetId, vetName, _) = GetCaller();
        var result = await _svc.AddRecordAsync(id, req, vetId, vetName, ct);
        return CreatedAtAction(nameof(GetRecord), new { id, recordId = result.Id }, result);
    }

    [HttpGet("{id:guid}/records")]
    public async Task<IActionResult> ListRecords(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        Guid? ownerFilter = IsOwner() ? GetCaller().Id : null;
        var result = await _svc.ListRecordsAsync(id, ownerFilter, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/records/{recordId:guid}")]
    public async Task<IActionResult> GetRecord(Guid id, Guid recordId, CancellationToken ct)
    {
        Guid? ownerFilter = IsOwner() ? GetCaller().Id : null;
        var result = await _svc.GetRecordAsync(id, recordId, ownerFilter, ct);
        return Ok(result);
    }

    private bool IsOwner() =>
        User.IsInRole("Owner");

    private (Guid Id, string Name, string Phone) GetCaller()
    {
        var sub   = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Token inválido.");
        var name  = User.FindFirstValue(ClaimTypes.Name)
                 ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Name)
                 ?? User.FindFirstValue("email")
                 ?? "Desconocido";
        var phone = User.FindFirstValue("phone") ?? "";
        return (Guid.Parse(sub), name, phone);
    }
}
