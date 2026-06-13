using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientsService.Application.DTOs;
using PatientsService.Application.Services;

namespace PatientsService.API.Controllers;

/// <summary>
/// Registro y gestión de mascotas e historias clínicas.
/// Un <c>Owner</c> solo puede acceder y modificar sus propias mascotas.
/// Un <c>Veterinarian</c> o <c>Admin</c> puede gestionar todas las mascotas del sistema.
/// </summary>
[ApiController]
[Route("api/patients")]
[Authorize]
[Produces("application/json")]
public class PatientsController : ControllerBase
{
    private readonly PatientAppService _svc;

    public PatientsController(PatientAppService svc) => _svc = svc;

    /// <summary>
    /// Registra una nueva mascota. El comportamiento varía según el rol:
    /// <list type="bullet">
    /// <item><description><b>Owner:</b> el dueño se toma automáticamente del JWT. No debe enviar <c>ownerId</c>.</description></item>
    /// <item><description><b>Veterinarian / Admin:</b> deben enviar <c>ownerId</c>, <c>ownerName</c> y <c>ownerPhone</c> obligatoriamente.</description></item>
    /// </list>
    /// </summary>
    /// <param name="req">Datos de la nueva mascota.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Mascota creada con todos sus datos y datos del dueño.</returns>
    /// <response code="201">Mascota registrada correctamente.</response>
    /// <response code="400">Datos inválidos o falta <c>ownerId</c>/<c>ownerName</c> para rol Veterinarian.</response>
    /// <response code="401">No autenticado.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Lista mascotas con filtros y paginación.
    /// Un <c>Owner</c> solo recibe sus propias mascotas independientemente de los filtros enviados.
    /// </summary>
    /// <param name="page">Número de página (default 1).</param>
    /// <param name="pageSize">Registros por página (default 20, máx 100).</param>
    /// <param name="species">Filtrar por especie: <c>dog</c>, <c>cat</c>, <c>bird</c>, <c>rabbit</c>, <c>other</c>.</param>
    /// <param name="search">Búsqueda parcial por nombre de mascota o nombre del dueño.</param>
    /// <param name="ownerId">Filtrar por dueño específico (solo para Veterinarian/Admin).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Página de mascotas con conteo total.</returns>
    /// <response code="200">Lista de mascotas devuelta correctamente.</response>
    /// <response code="401">No autenticado.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PatientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
        [FromQuery] int    page     = 1,
        [FromQuery] int    pageSize = 20,
        [FromQuery] string? species = null,
        [FromQuery] string? search  = null,
        [FromQuery] Guid?   ownerId = null,
        CancellationToken ct = default)
    {
        // owner solo ve las suyas; veterinarian ve todas o filtra por dueño específico
        Guid? ownerFilter = IsOwner() ? GetCaller().Id : ownerId;
        var result = await _svc.ListAsync(ownerFilter, species, search, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Obtiene una mascota por su ID.</summary>
    /// <param name="id">ID (GUID) de la mascota.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Datos completos de la mascota incluyendo información del dueño.</returns>
    /// <response code="200">Mascota encontrada.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">El Owner intenta acceder a una mascota que no le pertenece.</response>
    /// <response code="404">Mascota no encontrada.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        Guid? ownerFilter = IsOwner() ? GetCaller().Id : null;
        var result = await _svc.GetAsync(id, ownerFilter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Actualiza los datos de una mascota. La especie no es modificable una vez registrada.
    /// Un <c>Owner</c> solo puede editar sus propias mascotas.
    /// </summary>
    /// <param name="id">ID (GUID) de la mascota.</param>
    /// <param name="req">Datos a actualizar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Mascota con los datos actualizados.</returns>
    /// <response code="200">Mascota actualizada correctamente.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Sin permiso sobre esta mascota.</response>
    /// <response code="404">Mascota no encontrada.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdatePatientRequest req, CancellationToken ct)
    {
        Guid? ownerFilter = IsOwner() ? GetCaller().Id : null;
        var result = await _svc.UpdateAsync(id, req, ownerFilter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Elimina una mascota y toda su historia clínica asociada de forma permanente.
    /// Esta es la única operación de borrado físico permitida en el sistema.
    /// Solo disponible para Veterinarian y Admin.
    /// </summary>
    /// <param name="id">ID (GUID) de la mascota a eliminar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <response code="204">Mascota y su historia clínica eliminadas correctamente.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Veterinarian y Admin).</response>
    /// <response code="404">Mascota no encontrada.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Veterinarian,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    // ── Historia Clínica ──────────────────────────────────────────────────────────

    /// <summary>
    /// Agrega una nueva entrada a la historia clínica de una mascota.
    /// La historia clínica es el registro médico oficial y permanente — se diferencia de la
    /// bitácora de consulta en que es un resumen estructurado redactado por el veterinario.
    /// Solo disponible para Veterinarian y Admin.
    /// </summary>
    /// <param name="id">ID (GUID) de la mascota.</param>
    /// <param name="req">Datos de la entrada clínica: diagnóstico, tratamiento, notas, etc.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Entrada clínica creada con el nombre del veterinario que la registró.</returns>
    /// <response code="201">Entrada clínica creada correctamente.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Rol no autorizado (solo Veterinarian y Admin).</response>
    /// <response code="404">Mascota no encontrada.</response>
    [HttpPost("{id:guid}/records")]
    [Authorize(Roles = "Veterinarian,Admin")]
    [ProducesResponseType(typeof(ClinicalRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddRecord(
        Guid id, [FromBody] CreateClinicalRecordRequest req, CancellationToken ct)
    {
        var (vetId, vetName, _) = GetCaller();
        var result = await _svc.AddRecordAsync(id, req, vetId, vetName, ct);
        return CreatedAtAction(nameof(GetRecord), new { id, recordId = result.Id }, result);
    }

    /// <summary>
    /// Obtiene la historia clínica completa de una mascota, ordenada del más reciente al más antiguo.
    /// Un <c>Owner</c> solo puede consultar la historia de sus propias mascotas.
    /// </summary>
    /// <param name="id">ID (GUID) de la mascota.</param>
    /// <param name="page">Número de página (default 1).</param>
    /// <param name="pageSize">Registros por página (default 20).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Página con entradas de la historia clínica.</returns>
    /// <response code="200">Historia clínica devuelta correctamente.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Sin permiso sobre esta mascota.</response>
    /// <response code="404">Mascota no encontrada.</response>
    [HttpGet("{id:guid}/records")]
    [ProducesResponseType(typeof(PagedResponse<ClinicalRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>Obtiene una entrada específica de la historia clínica de una mascota.</summary>
    /// <param name="id">ID (GUID) de la mascota.</param>
    /// <param name="recordId">ID (GUID) de la entrada clínica.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Entrada clínica con diagnóstico, tratamiento, signos vitales y nombre del veterinario.</returns>
    /// <response code="200">Entrada encontrada.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">Sin permiso sobre esta mascota.</response>
    /// <response code="404">Mascota o entrada clínica no encontrada.</response>
    [HttpGet("{id:guid}/records/{recordId:guid}")]
    [ProducesResponseType(typeof(ClinicalRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
                 ?? User.FindFirstValue(JwtRegisteredClaimNames.Name)
                 ?? User.FindFirstValue("email")
                 ?? "Desconocido";
        var phone = User.FindFirstValue("phone") ?? "";
        return (Guid.Parse(sub), name, phone);
    }
}
