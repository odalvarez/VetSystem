using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientsService.Application.DTOs;
using PatientsService.Application.Services;

namespace PatientsService.API.Controllers;

/// <summary>
/// Gestión del catálogo de especies disponibles en el sistema.
/// La lectura está disponible para todos los roles autenticados.
/// La creación, edición y eliminación son exclusivas del rol <c>Admin</c>.
/// </summary>
[ApiController]
[Route("api/species")]
[Authorize]
[Produces("application/json")]
public class SpeciesController : ControllerBase
{
    private readonly SpeciesAppService _svc;

    public SpeciesController(SpeciesAppService svc) => _svc = svc;

    /// <summary>
    /// Lista todas las especies disponibles en el sistema.
    /// Usado para poblar selects en los formularios de registro de mascotas.
    /// Disponible para todos los roles autenticados.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lista completa de especies con ID, nombre y código.</returns>
    /// <response code="200">Lista de especies devuelta correctamente.</response>
    /// <response code="401">No autenticado.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SpeciesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _svc.ListAsync(ct));

    /// <summary>
    /// Crea una nueva especie en el catálogo.
    /// Solo disponible para Admin.
    /// </summary>
    /// <param name="req">Nombre y código de la nueva especie.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Especie creada con su ID asignado.</returns>
    /// <response code="201">Especie creada correctamente.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Admin).</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SpeciesResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateSpeciesRequest req, CancellationToken ct)
    {
        var result = await _svc.CreateAsync(req, ct);
        return CreatedAtAction(nameof(List), new { }, result);
    }

    /// <summary>
    /// Actualiza el nombre o código de una especie existente.
    /// Solo disponible para Admin.
    /// </summary>
    /// <param name="id">ID (GUID) de la especie.</param>
    /// <param name="req">Nuevos datos de la especie.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Especie con los datos actualizados.</returns>
    /// <response code="200">Especie actualizada correctamente.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Admin).</response>
    /// <response code="404">Especie no encontrada.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SpeciesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSpeciesRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, req, ct));

    /// <summary>
    /// Elimina una especie del catálogo.
    /// Solo disponible para Admin. No se puede eliminar una especie si hay mascotas registradas con ella.
    /// </summary>
    /// <param name="id">ID (GUID) de la especie a eliminar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="204">Especie eliminada correctamente.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Admin).</response>
    /// <response code="404">Especie no encontrada.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
