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
            new { Status = "Confirmed" });

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
            new { Status = "Confirmed" });

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
            new { Status = "Confirmed" });

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
}
