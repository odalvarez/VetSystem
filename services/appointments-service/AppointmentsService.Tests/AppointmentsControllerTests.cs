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
        var client = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp   = await client.GetAsync("/api/appointments");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ListAppointments_AsOwner_Returns200()
    {
        var client = ClientAs(AppointmentsWebFactory.OwnerId, "owner@test.com", "Owner");
        var resp   = await client.GetAsync("/api/appointments");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ListAppointments_AsAdmin_Returns200()
    {
        var client = ClientAs(AppointmentsWebFactory.AdminId, "admin@test.com", "Admin");
        var resp   = await client.GetAsync("/api/appointments");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Crear cita ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAppointment_AsVet_Returns201()
    {
        var client  = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var patientId = Guid.NewGuid();

        var resp = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = patientId,
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

        var resp = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
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
        // El owner puede crear cita para sus propias mascotas
        var client = ClientAs(AppointmentsWebFactory.OwnerId, "owner@test.com", "Owner");

        var resp = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
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
        var client    = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var scheduledAt = DateTime.UtcNow.AddDays(10).Date.AddHours(10);
        var vetId     = Guid.NewGuid(); // nuevo vet para evitar interferencia con otros tests

        var base_req = new CreateAppointmentRequest
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

        await client.PostAsJsonAsync("/api/appointments", base_req);

        // Segunda cita en el mismo vet y mismo slot
        base_req.PatientName = "Mascota B";
        base_req.Reason      = "Segunda cita — debería fallar";
        var resp2 = await client.PostAsJsonAsync("/api/appointments", base_req);

        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
    }

    // ── Cambio de estado ──────────────────────────────────────────────────────

    [Fact]
    public async Task ChangeStatus_AsVet_Returns200()
    {
        var client    = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var vetId     = Guid.NewGuid();
        var createReq = new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = vetId,
            VeterinarianName = "Status Vet",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Luna",
            ScheduledAt      = DateTime.UtcNow.AddDays(4),
            DurationMinutes  = 30,
            Reason           = "Revisión"
        };

        var createResp = await client.PostAsJsonAsync("/api/appointments", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var created    = await createResp.Content.ReadFromJsonAsync<AppointmentResponse>();
        var statusResp = await client.PatchAsJsonAsync(
            $"/api/appointments/{created!.Id}/status",
            new { Status = "Confirmed" });

        Assert.Equal(HttpStatusCode.OK, statusResp.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_AsOwner_Returns403()
    {
        // El owner no puede cambiar estados
        var vetClient  = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var vetId      = Guid.NewGuid();
        var createResp = await vetClient.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = vetId,
            VeterinarianName = "Perm Vet",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Max",
            ScheduledAt      = DateTime.UtcNow.AddDays(6),
            DurationMinutes  = 30,
            Reason           = "Vacuna"
        });

        var created    = await createResp.Content.ReadFromJsonAsync<AppointmentResponse>();
        var ownerClient = ClientAs(AppointmentsWebFactory.OwnerId, "owner@test.com", "Owner");
        var statusResp = await ownerClient.PatchAsJsonAsync(
            $"/api/appointments/{created!.Id}/status",
            new { Status = "Confirmed" });

        Assert.Equal(HttpStatusCode.Forbidden, statusResp.StatusCode);
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
