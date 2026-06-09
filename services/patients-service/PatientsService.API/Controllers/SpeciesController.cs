using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientsService.Application.DTOs;
using PatientsService.Application.Services;

namespace PatientsService.API.Controllers;

[ApiController]
[Route("api/species")]
[Authorize]
public class SpeciesController : ControllerBase
{
    private readonly SpeciesAppService _svc;

    public SpeciesController(SpeciesAppService svc) => _svc = svc;

    // Cualquier usuario autenticado puede listar las especies (para los formularios)
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _svc.ListAsync(ct));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateSpeciesRequest req, CancellationToken ct)
    {
        var result = await _svc.CreateAsync(req, ct);
        return CreatedAtAction(nameof(List), new { }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSpeciesRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, req, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
