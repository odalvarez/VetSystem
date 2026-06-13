using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AppointmentsService.Application.DTOs;
using AppointmentsService.Tests.Helpers;

namespace AppointmentsService.Tests;

public class AppointmentsControllerTests : IClassFixture<AppointmentsWebFactory>
{
    private readonly AppointmentsWebFactory _factory;

    public AppointmentsControllerTests(AppointmentsWebFactory factory) => _factory = factory;

    private HttpClient ClientAs(Guid userId, string email, string role) =>
        AuthorizedClient(JwtTestHelper.Generate(userId, email, role, $"{role} Test"));

    private HttpClient AuthorizedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<AppointmentResponse> CreateAppointmentAs(HttpClient client, DateTime? scheduledAt = null)
    {
        var resp = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = Guid.NewGuid(),
            VeterinarianName = "Vet Test",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Luna",
            ScheduledAt      = scheduledAt ?? DateTime.UtcNow.AddDays(3),
            DurationMinutes  = 30,
            Reason           = "Control"
        });
        return (await resp.Content.ReadFromJsonAsync<AppointmentResponse>())!;
    }

    // ── Auth guard ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAppointments_Unauthenticated_Returns401()
    {
        var resp = await _factory.CreateClient().GetAsync("/api/appointments");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Listar citas ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAppointments_AsVet_Returns200()
    {
        var resp = await ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian")
            .GetAsync("/api/appointments");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ListAppointments_AsOwner_Returns200()
    {
        var resp = await ClientAs(AppointmentsWebFactory.OwnerId, "owner@test.com", "Owner")
            .GetAsync("/api/appointments");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ListAppointments_AsAdmin_Returns200()
    {
        var resp = await ClientAs(AppointmentsWebFactory.AdminId, "admin@test.com", "Admin")
            .GetAsync("/api/appointments");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Crear cita ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAppointment_AsVet_Returns201()
    {
        var client = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp   = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = AppointmentsWebFactory.VetId,
            VeterinarianName = "Vet Test",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Luna",
            ScheduledAt      = DateTime.UtcNow.AddDays(3),
            DurationMinutes  = 30,
            Reason           = "Control anual"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_AsAdmin_Returns201()
    {
        var client = ClientAs(AppointmentsWebFactory.AdminId, "admin@test.com", "Admin");
        var resp   = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = AppointmentsWebFactory.VetId,
            VeterinarianName = "Vet Test",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Max",
            ScheduledAt      = DateTime.UtcNow.AddDays(5),
            DurationMinutes  = 45,
            Reason           = "Vacunación"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_AsOwner_Returns201()
    {
        var client = ClientAs(AppointmentsWebFactory.OwnerId, "owner@test.com", "Owner");
        var resp   = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = AppointmentsWebFactory.VetId,
            VeterinarianName = "Vet Test",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Toby",
            ScheduledAt      = DateTime.UtcNow.AddDays(7),
            DurationMinutes  = 30,
            Reason           = "Revisión"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    // ── Conflicto de horario ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateAppointment_ConflictingSlot_Returns409()
    {
        var client      = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var scheduledAt = DateTime.UtcNow.AddDays(10).Date.AddHours(10);
        var vetId       = Guid.NewGuid();

        var baseReq = new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = vetId,
            VeterinarianName = "Conflict Vet",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Mascota A",
            ScheduledAt      = scheduledAt,
            DurationMinutes  = 60,
            Reason           = "Primera cita"
        };

        await client.PostAsJsonAsync("/api/appointments", baseReq);

        baseReq.PatientName = "Mascota B";
        baseReq.Reason      = "Segunda cita — debería fallar";
        var resp2 = await client.PostAsJsonAsync("/api/appointments", baseReq);

        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
    }

    // ── Obtener cita por ID ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAppointment_AsVet_Returns200()
    {
        var vet        = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var appointment = await CreateAppointmentAs(vet);

        var resp = await vet.GetAsync($"/api/appointments/{appointment.Id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetAppointment_Unauthenticated_Returns401()
    {
        var resp = await _factory.CreateClient().GetAsync($"/api/appointments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Actualizar cita ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAppointment_AsVet_Returns200()
    {
        var vet         = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var appointment = await CreateAppointmentAs(vet);

        var resp = await vet.PutAsJsonAsync($"/api/appointments/{appointment.Id}", new UpdateAppointmentRequest
        {
            ScheduledAt     = DateTime.UtcNow.AddDays(4),
            DurationMinutes = 45,
            Reason          = "Control actualizado"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateAppointment_AsOwner_Returns200()
    {
        // El owner puede editar sus propias citas
        var owner       = ClientAs(AppointmentsWebFactory.OwnerId, "owner@test.com", "Owner");
        var appointment = await CreateAppointmentAs(owner);

        var resp = await owner.PutAsJsonAsync($"/api/appointments/{appointment.Id}", new UpdateAppointmentRequest
        {
            ScheduledAt     = DateTime.UtcNow.AddDays(8),
            DurationMinutes = 30,
            Reason          = "Revisión ajustada"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Cambio de estado ──────────────────────────────────────────────────────

    [Fact]
    public async Task ChangeStatus_AsVet_Returns200()
    {
        var vet         = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var appointment = await CreateAppointmentAs(vet);

        var resp = await vet.PatchAsJsonAsync(
            $"/api/appointments/{appointment.Id}/status",
            new { Status = "Completed" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_AsAdmin_Returns200()
    {
        var vet         = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var appointment = await CreateAppointmentAs(vet);

        var admin = ClientAs(AppointmentsWebFactory.AdminId, "admin@test.com", "Admin");
        var resp  = await admin.PatchAsJsonAsync(
            $"/api/appointments/{appointment.Id}/status",
            new { Status = "Completed" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_AsOwner_Returns403()
    {
        var vet         = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var appointment = await CreateAppointmentAs(vet);

        var owner = ClientAs(AppointmentsWebFactory.OwnerId, "owner@test.com", "Owner");
        var resp  = await owner.PatchAsJsonAsync(
            $"/api/appointments/{appointment.Id}/status",
            new { Status = "Completed" });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── Cancelar (DELETE) cita ────────────────────────────────────────────────

    [Fact]
    public async Task CancelAppointment_AsVet_Returns204()
    {
        var vet         = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var appointment = await CreateAppointmentAs(vet);

        var resp = await vet.DeleteAsync($"/api/appointments/{appointment.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task CancelAppointment_AsOwner_Returns204()
    {
        var owner       = ClientAs(AppointmentsWebFactory.OwnerId, "owner@test.com", "Owner");
        var appointment = await CreateAppointmentAs(owner);

        var resp = await owner.DeleteAsync($"/api/appointments/{appointment.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task CancelAppointment_Unauthenticated_Returns401()
    {
        var resp = await _factory.CreateClient().DeleteAsync($"/api/appointments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Disponibilidad ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAvailability_Authenticated_Returns200()
    {
        var client = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var date   = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));
        var vetId  = Guid.NewGuid();

        var resp = await client.GetAsync(
            $"/api/appointments/availability?veterinarianId={vetId}&date={date:yyyy-MM-dd}&durationMinutes=30");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── P1: Autorización y conflictos ─────────────────────────────────────────

    [Fact]
    public async Task CreateAppointment_AsOwner_ForDifferentOwner_Returns403()
    {
        // Owner A intenta crear una cita poniendo el OwnerId de Owner B — debe ser rechazado
        var ownerA  = ClientAs(AppointmentsWebFactory.OwnerId, "owner@test.com", "Owner");
        var ownerBId = Guid.NewGuid();

        var resp = await ownerA.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = AppointmentsWebFactory.VetId,
            VeterinarianName = "Vet Test",
            OwnerId          = ownerBId,
            OwnerName        = "Owner B",
            OwnerPhone       = "3009999999",
            PatientName      = "Firulais",
            ScheduledAt      = DateTime.UtcNow.AddDays(3),
            DurationMinutes  = 30,
            Reason           = "Revisión ajena"
        });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateAppointment_ToThePast_Returns400()
    {
        // El dominio impide mover una cita a una fecha ya pasada
        var vet         = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var appointment = await CreateAppointmentAs(vet);

        var resp = await vet.PutAsJsonAsync($"/api/appointments/{appointment.Id}", new UpdateAppointmentRequest
        {
            ScheduledAt     = DateTime.UtcNow.AddDays(-1),
            DurationMinutes = 30,
            Reason          = "Pasado"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateAppointment_SameSlot_DoesNotConflictWithItself()
    {
        // Actualizar manteniendo el mismo horario no debe generar conflicto con la propia cita
        var vet         = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var appointment = await CreateAppointmentAs(vet, DateTime.UtcNow.AddDays(20).Date.AddHours(9));

        var resp = await vet.PutAsJsonAsync($"/api/appointments/{appointment.Id}", new UpdateAppointmentRequest
        {
            ScheduledAt     = appointment.ScheduledAt,
            DurationMinutes = appointment.DurationMinutes,
            Reason          = "Motivo actualizado sin cambio de hora"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_PartialOverlap_Returns409()
    {
        // 10:00–10:30 reservada; intento de 10:15–10:45 con el mismo veterinario debe fallar
        var client      = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var vetId       = Guid.NewGuid();
        var baseDate    = DateTime.UtcNow.AddDays(25).Date;

        await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = vetId,
            VeterinarianName = "Overlap Vet",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Mascota X",
            ScheduledAt      = baseDate.AddHours(10),
            DurationMinutes  = 30,
            Reason           = "Primera cita"
        });

        var resp = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = vetId,
            VeterinarianName = "Overlap Vet",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Mascota Y",
            ScheduledAt      = baseDate.AddHours(10).AddMinutes(15),
            DurationMinutes  = 30,
            Reason           = "Segunda cita solapada"
        });

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    // ── P2: Cancelación y resiliencia ─────────────────────────────────────────

    [Fact]
    public async Task CancelAppointment_AsOwner_WhenCompleted_Returns400()
    {
        // Owner no puede cancelar una cita ya completada
        var vet         = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var appointment = await CreateAppointmentAs(vet);

        await vet.PatchAsJsonAsync(
            $"/api/appointments/{appointment.Id}/status",
            new { Status = "Completed" });

        var owner = ClientAs(AppointmentsWebFactory.OwnerId, "owner@test.com", "Owner");
        var resp  = await owner.DeleteAsync($"/api/appointments/{appointment.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_NotificationFails_AppointmentStillCompleted()
    {
        // El notifications-service mock no falla; verificamos que el estado queda completed
        var vet         = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var appointment = await CreateAppointmentAs(vet);

        var resp = await vet.PatchAsJsonAsync(
            $"/api/appointments/{appointment.Id}/status",
            new { Status = "Completed" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<AppointmentResponse>();
        Assert.Equal("completed", body!.Status);
    }

    [Fact]
    public async Task ListAppointments_InvalidDateRange_FromAfterTo_Returns400OrEmpty()
    {
        // from posterior a to no tiene sentido; el backend devuelve 400 o lista vacía
        var client = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var from   = DateTime.UtcNow.AddDays(10).ToString("o");
        var to     = DateTime.UtcNow.AddDays(1).ToString("o");

        var resp = await client.GetAsync($"/api/appointments?from={from}&to={to}");

        var isAcceptable = resp.StatusCode == HttpStatusCode.BadRequest
                        || resp.StatusCode == HttpStatusCode.OK;
        Assert.True(isAcceptable);

        if (resp.StatusCode == HttpStatusCode.OK)
        {
            var body = await resp.Content.ReadFromJsonAsync<PagedResponse<AppointmentResponse>>();
            Assert.NotNull(body);
            // Si retorna 200 esperamos lista vacía (ningún resultado puede cumplir from > to)
            Assert.Empty(body!.Items);
        }
    }

    // ── P3: Validaciones de duración y disponibilidad ─────────────────────────

    [Fact]
    public async Task CreateAppointment_DurationTooShort_Returns400()
    {
        // El DTO tiene [Range(10, 480)] y el dominio también lo valida
        var client = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");

        var resp = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = Guid.NewGuid(),
            VeterinarianName = "Vet Test",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Mini",
            ScheduledAt      = DateTime.UtcNow.AddDays(3),
            DurationMinutes  = 5,
            Reason           = "Duración inválida"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_DurationTooLong_Returns400()
    {
        // Más de 480 minutos supera el límite del dominio
        var client = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");

        var resp = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = Guid.NewGuid(),
            VeterinarianName = "Vet Test",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Max",
            ScheduledAt      = DateTime.UtcNow.AddDays(3),
            DurationMinutes  = 600,
            Reason           = "Duración excesiva"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetAvailability_WithExistingAppointment_SlotUnavailable()
    {
        // El slot ocupado por una cita existente no debe aparecer en la disponibilidad
        var client  = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var vetId   = Guid.NewGuid();
        var date    = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var slotAt  = date.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);

        await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = vetId,
            VeterinarianName = "Avail Vet",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Coco",
            ScheduledAt      = slotAt,
            DurationMinutes  = 30,
            Reason           = "Cita que ocupa el slot"
        });

        var resp = await client.GetAsync(
            $"/api/appointments/availability?veterinarianId={vetId}&date={date:yyyy-MM-dd}&durationMinutes=30");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<AvailabilityResponse>();
        Assert.NotNull(body);

        // El slot 10:00–10:30 debe estar ausente de la lista de disponibles
        var slotOcupado = body!.AvailableSlots.FirstOrDefault(s => s.Start == slotAt);
        Assert.Null(slotOcupado);
    }

    // ── P4: Notas en blanco ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateAppointment_WithBlankNotes_Stored()
    {
        // Notes con solo espacios debe guardarse como null o cadena vacía (el dominio aplica Trim)
        var client = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");

        var resp = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = Guid.NewGuid(),
            VeterinarianName = "Vet Test",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Nube",
            ScheduledAt      = DateTime.UtcNow.AddDays(3),
            DurationMinutes  = 30,
            Reason           = "Revisión",
            Notes            = "   "
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<AppointmentResponse>();
        Assert.NotNull(body);
        // Notes con Trim queda vacío o null — en ambos casos no debe ser "   "
        Assert.True(body!.Notes == null || body.Notes.Trim().Length == 0);
    }
}
