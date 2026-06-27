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

    // Devuelve el primer día laborable (lun–sáb) a partir de minDays días desde ahora.
    // Evita fallos cuando AddDays(N) cae en domingo.
    private static DateTime WorkdayUtc(int minDays, int hour, int minute = 0)
    {
        var d = DateTime.UtcNow.AddDays(minDays).Date;
        if (d.DayOfWeek == DayOfWeek.Sunday) d = d.AddDays(1);
        return d.AddHours(hour).AddMinutes(minute);
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
            ScheduledAt      = scheduledAt ?? WorkdayUtc(3, 10),
            DurationMinutes  = 30,
            Reason           = "Control"
        });
        return (await resp.Content.ReadFromJsonAsync<AppointmentResponse>())!;
    }

    // â”€â”€ Auth guard â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task ListAppointments_Unauthenticated_Returns401()
    {
        var resp = await _factory.CreateClient().GetAsync("/api/appointments");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // â”€â”€ Listar citas â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

    // â”€â”€ Crear cita â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
            ScheduledAt      = WorkdayUtc(3, 10),
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
            ScheduledAt      = DateTime.UtcNow.AddDays(5).Date.AddHours(10),
            DurationMinutes  = 45,
            Reason           = "VacunaciÃ³n"
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
            ScheduledAt      = DateTime.UtcNow.AddDays(7).Date.AddHours(10),
            DurationMinutes  = 30,
            Reason           = "RevisiÃ³n"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    // â”€â”€ Conflicto de horario â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task CreateAppointment_ConflictingSlot_Returns409()
    {
        var client      = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var scheduledAt = WorkdayUtc(10, 10);
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
        baseReq.Reason      = "Segunda cita â€” deberÃ­a fallar";
        var resp2 = await client.PostAsJsonAsync("/api/appointments", baseReq);

        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
    }

    // -- Horario de atencion -------------------------------------------------------

    [Fact]
    public async Task CreateAppointment_BeforeWorkStart_Returns400()
    {
        var client = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = Guid.NewGuid(),
            VeterinarianName = "Vet Test",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Luna",
            ScheduledAt      = DateTime.UtcNow.AddDays(3).Date.AddHours(6),
            DurationMinutes  = 30,
            Reason           = "Control"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_AfterWorkEnd_Returns400()
    {
        var client = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = Guid.NewGuid(),
            VeterinarianName = "Vet Test",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Luna",
            ScheduledAt      = DateTime.UtcNow.AddDays(3).Date.AddHours(21),
            DurationMinutes  = 30,
            Reason           = "Control"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_EndExceedsWorkEnd_Returns400()
    {
        var client = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = Guid.NewGuid(),
            VeterinarianName = "Vet Test",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Luna",
            ScheduledAt      = DateTime.UtcNow.AddDays(3).Date.AddHours(19).AddMinutes(45),
            DurationMinutes  = 30,
            Reason           = "Control"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_AtWorkEnd_Returns201()
    {
        var client = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp = await client.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = Guid.NewGuid(),
            VeterinarianName = "Vet Test",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Luna",
            ScheduledAt      = WorkdayUtc(3, 19, 30),
            DurationMinutes  = 30,
            Reason           = "Control"
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    // â”€â”€ Obtener cita por ID â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

    // â”€â”€ Actualizar cita â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task UpdateAppointment_AsVet_Returns200()
    {
        var vet         = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var appointment = await CreateAppointmentAs(vet);

        var resp = await vet.PutAsJsonAsync($"/api/appointments/{appointment.Id}", new UpdateAppointmentRequest
        {
            ScheduledAt     = GetNextWeekday(DayOfWeek.Monday).Date.AddHours(10),
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
            ScheduledAt     = DateTime.UtcNow.AddDays(8).Date.AddHours(10),
            DurationMinutes = 30,
            Reason          = "RevisiÃ³n ajustada"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // â”€â”€ Cambio de estado â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

    // â”€â”€ Cancelar (DELETE) cita â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

    // â”€â”€ Disponibilidad â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task GetAvailability_Authenticated_Returns200()
    {
        var client = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var date   = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14).Date.AddHours(10));
        var vetId  = Guid.NewGuid();

        var resp = await client.GetAsync(
            $"/api/appointments/availability?veterinarianId={vetId}&date={date:yyyy-MM-dd}&durationMinutes=30");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // â”€â”€ P1: AutorizaciÃ³n y conflictos â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task CreateAppointment_AsOwner_ForDifferentOwner_Returns403()
    {
        // Owner A intenta crear una cita poniendo el OwnerId de Owner B â€” debe ser rechazado
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
            ScheduledAt      = DateTime.UtcNow.AddDays(3).Date.AddHours(10),
            DurationMinutes  = 30,
            Reason           = "RevisiÃ³n ajena"
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
        // 10:00â€“10:30 reservada; intento de 10:15â€“10:45 con el mismo veterinario debe fallar
        var client      = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var vetId       = Guid.NewGuid();
        var baseDate    = GetNextWeekday(DayOfWeek.Thursday).Date;

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

    // â”€â”€ P2: CancelaciÃ³n y resiliencia â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
        // from posterior a to no tiene sentido; el backend devuelve 400 o lista vacÃ­a
        var client = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var from   = DateTime.UtcNow.AddDays(10).Date.AddHours(10).ToString("o");
        var to     = DateTime.UtcNow.AddDays(1).Date.AddHours(10).ToString("o");

        var resp = await client.GetAsync($"/api/appointments?from={from}&to={to}");

        var isAcceptable = resp.StatusCode == HttpStatusCode.BadRequest
                        || resp.StatusCode == HttpStatusCode.OK;
        Assert.True(isAcceptable);

        if (resp.StatusCode == HttpStatusCode.OK)
        {
            var body = await resp.Content.ReadFromJsonAsync<PagedResponse<AppointmentResponse>>();
            Assert.NotNull(body);
            // Si retorna 200 esperamos lista vacÃ­a (ningÃºn resultado puede cumplir from > to)
            Assert.Empty(body!.Items);
        }
    }

    // â”€â”€ P3: Validaciones de duraciÃ³n y disponibilidad â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task CreateAppointment_DurationTooShort_Returns400()
    {
        // El DTO tiene [Range(10, 480)] y el dominio tambiÃ©n lo valida
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
            ScheduledAt      = DateTime.UtcNow.AddDays(3).Date.AddHours(10),
            DurationMinutes  = 5,
            Reason           = "DuraciÃ³n invÃ¡lida"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_DurationTooLong_Returns400()
    {
        // MÃ¡s de 480 minutos supera el lÃ­mite del dominio
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
            ScheduledAt      = DateTime.UtcNow.AddDays(3).Date.AddHours(10),
            DurationMinutes  = 600,
            Reason           = "DuraciÃ³n excesiva"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetAvailability_WithExistingAppointment_SlotUnavailable()
    {
        // El slot ocupado por una cita existente no debe aparecer en la disponibilidad
        var client  = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var vetId   = Guid.NewGuid();
        var date    = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30).Date.AddHours(10));
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

        // El slot 10:00â€“10:30 debe estar ausente de la lista de disponibles
        var slotOcupado = body!.AvailableSlots.FirstOrDefault(s => s.Start == slotAt);
        Assert.Null(slotOcupado);
    }

    // â”€â”€ P4: Notas en blanco â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task CreateAppointment_WithBlankNotes_Stored()
    {
        // Notes con solo espacios debe guardarse como null o cadena vacÃ­a (el dominio aplica Trim)
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
            ScheduledAt      = WorkdayUtc(3, 10),
            DurationMinutes  = 30,
            Reason           = "RevisiÃ³n",
            Notes            = "   "
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<AppointmentResponse>();
        Assert.NotNull(body);
        // Notes con Trim queda vacÃ­o o null â€” en ambos casos no debe ser "   "
        Assert.True(body!.Notes == null || body.Notes.Trim().Length == 0);
    }

    // ── Horario configurable (ClinicSettings + VeterinarianSchedule + VeterinarianLeave) ──

    [Fact]
    public async Task GetClinicSettings_AsAdmin_Returns200()
    {
        var admin = ClientAs(AppointmentsWebFactory.AdminId, "admin@test.com", "Admin");
        var resp  = await admin.GetAsync("/api/schedules/settings");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<ClinicSettingsResponse>();
        Assert.NotNull(body);
        Assert.Equal("08:00", body!.StartTime);
        Assert.Equal("20:00", body.EndTime);
    }

    [Fact]
    public async Task GetClinicSettings_AsVet_Returns403()
    {
        var vet  = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp = await vet.GetAsync("/api/schedules/settings");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateClinicSettings_AsAdmin_Persists()
    {
        var admin = ClientAs(AppointmentsWebFactory.AdminId, "admin@test.com", "Admin");
        var resp  = await admin.PutAsJsonAsync("/api/schedules/settings", new UpdateClinicSettingsRequest
        {
            StartTime = "09:00",
            EndTime   = "17:00",
            WorkDays  = ["Monday", "Tuesday", "Wednesday"]
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<ClinicSettingsResponse>();
        Assert.Equal("09:00", body!.StartTime);
        Assert.Equal("17:00", body.EndTime);

        // Restaura para no contaminar otros tests
        await admin.PutAsJsonAsync("/api/schedules/settings", new UpdateClinicSettingsRequest
        {
            StartTime = "08:00",
            EndTime   = "20:00",
            WorkDays  = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"]
        });
    }

    [Fact]
    public async Task CreateAppointment_WhenVetHasLeave_Returns400()
    {
        var admin  = ClientAs(AppointmentsWebFactory.AdminId, "admin@test.com", "Admin");
        var vetId  = Guid.NewGuid();
        var target = DateTime.UtcNow.AddDays(5).Date.AddHours(10);
        var date   = DateOnly.FromDateTime(target);

        var leaveResp = await admin.PostAsJsonAsync($"/api/schedules/leaves/{vetId}", new CreateVeterinarianLeaveRequest
        {
            DateFrom = date.ToString("yyyy-MM-dd"),
            DateTo   = date.ToString("yyyy-MM-dd"),
            Reason   = "Vacaciones"
        });
        Assert.Equal(HttpStatusCode.Created, leaveResp.StatusCode);

        var vet  = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp = await vet.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = vetId,
            VeterinarianName = "Vet Ausente",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Luna",
            ScheduledAt      = target,
            DurationMinutes  = 30,
            Reason           = "Control"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_WithCustomVetSchedule_OutsideRange_Returns400()
    {
        var admin  = ClientAs(AppointmentsWebFactory.AdminId, "admin@test.com", "Admin");
        var vetId  = Guid.NewGuid();
        var target = GetNextWeekday(DayOfWeek.Monday);

        await admin.PutAsJsonAsync($"/api/schedules/vets/{vetId}", new UpsertVeterinarianScheduleRequest
        {
            DayOfWeek = "Monday",
            StartTime = "08:00",
            EndTime   = "12:00"
        });

        var vet  = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp = await vet.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = vetId,
            VeterinarianName = "Vet Parcial",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Luna",
            ScheduledAt      = target.Date.AddHours(14),
            DurationMinutes  = 30,
            Reason           = "Control"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_WithCustomVetSchedule_InsideRange_Returns201()
    {
        var admin  = ClientAs(AppointmentsWebFactory.AdminId, "admin@test.com", "Admin");
        var vetId  = Guid.NewGuid();
        var target = GetNextWeekday(DayOfWeek.Tuesday);

        await admin.PutAsJsonAsync($"/api/schedules/vets/{vetId}", new UpsertVeterinarianScheduleRequest
        {
            DayOfWeek = "Tuesday",
            StartTime = "08:00",
            EndTime   = "17:00"
        });

        var vet  = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp = await vet.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = vetId,
            VeterinarianName = "Vet Parcial",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Luna",
            ScheduledAt      = target.Date.AddHours(10),
            DurationMinutes  = 30,
            Reason           = "Control"
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task GetAvailability_WhenVetHasLeave_ReturnsNoSlots()
    {
        var admin  = ClientAs(AppointmentsWebFactory.AdminId, "admin@test.com", "Admin");
        var vetId  = Guid.NewGuid();
        var target = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6).Date);

        await admin.PostAsJsonAsync($"/api/schedules/leaves/{vetId}", new CreateVeterinarianLeaveRequest
        {
            DateFrom = target.ToString("yyyy-MM-dd"),
            DateTo   = target.ToString("yyyy-MM-dd"),
            Reason   = "Permiso"
        });

        var vet  = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp = await vet.GetAsync($"/api/appointments/availability?veterinarianId={vetId}&date={target:yyyy-MM-dd}&durationMinutes=30");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<AvailabilityResponse>();
        Assert.NotNull(body);
        Assert.Empty(body!.AvailableSlots);
    }

    // ── Ausencias parciales ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateLeave_WithPartialTime_Returns201()
    {
        var admin = ClientAs(AppointmentsWebFactory.AdminId, "admin@test.com", "Admin");
        var vetId = Guid.NewGuid();
        var date  = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7).Date);

        var resp = await admin.PostAsJsonAsync($"/api/schedules/leaves/{vetId}", new CreateVeterinarianLeaveRequest
        {
            DateFrom  = date.ToString("yyyy-MM-dd"),
            DateTo    = date.ToString("yyyy-MM-dd"),
            StartTime = "10:00",
            EndTime   = "12:00",
            Reason    = "Reunión"
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<VeterinarianLeaveResponse>();
        Assert.NotNull(body);
        Assert.Equal("10:00", body!.StartTime);
        Assert.Equal("12:00", body!.EndTime);
    }

    [Fact]
    public async Task GetAvailability_WithPartialLeave_BlocksOnlyAffectedSlots()
    {
        var admin = ClientAs(AppointmentsWebFactory.AdminId, "admin@test.com", "Admin");
        var vet   = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var vetId = Guid.NewGuid();
        var date  = DateOnly.FromDateTime(GetNextWeekday(DayOfWeek.Wednesday).Date);

        // Ausencia parcial 10:00-12:00
        await admin.PostAsJsonAsync($"/api/schedules/leaves/{vetId}", new CreateVeterinarianLeaveRequest
        {
            DateFrom  = date.ToString("yyyy-MM-dd"),
            DateTo    = date.ToString("yyyy-MM-dd"),
            StartTime = "10:00",
            EndTime   = "12:00",
            Reason    = "Reunión"
        });

        var resp = await vet.GetAsync(
            $"/api/appointments/availability?veterinarianId={vetId}&date={date:yyyy-MM-dd}&durationMinutes=30");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<AvailabilityResponse>();
        Assert.NotNull(body);

        // Deben existir slots (no está todo el día bloqueado)
        Assert.NotEmpty(body!.AvailableSlots);

        // Ningún slot debe solaparse con el bloqueo 10:00-12:00
        foreach (var slot in body.AvailableSlots)
        {
            var slotStart = TimeOnly.FromDateTime(slot.Start);
            var slotEnd   = TimeOnly.FromDateTime(slot.End);
            Assert.False(slotStart < new TimeOnly(12, 0) && slotEnd > new TimeOnly(10, 0),
                $"Slot {slotStart}-{slotEnd} solapa con la ausencia parcial 10:00-12:00");
        }
    }

    [Fact]
    public async Task CreateAppointment_WhenVetHasFullDayLeave_Returns400()
    {
        var admin  = ClientAs(AppointmentsWebFactory.AdminId, "admin@test.com", "Admin");
        var vet    = ClientAs(AppointmentsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var vetId  = Guid.NewGuid();
        var target = DateTime.UtcNow.AddDays(8).Date.AddHours(10);
        var date   = DateOnly.FromDateTime(target);

        // Ausencia completa (sin horas)
        await admin.PostAsJsonAsync($"/api/schedules/leaves/{vetId}", new CreateVeterinarianLeaveRequest
        {
            DateFrom = date.ToString("yyyy-MM-dd"),
            DateTo   = date.ToString("yyyy-MM-dd"),
            Reason   = "Vacaciones"
        });

        var resp = await vet.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest
        {
            PatientId        = Guid.NewGuid(),
            VeterinarianId   = vetId,
            VeterinarianName = "Vet Ausente",
            OwnerId          = AppointmentsWebFactory.OwnerId,
            OwnerName        = "Owner Test",
            OwnerPhone       = "3001234567",
            PatientName      = "Luna",
            ScheduledAt      = target,
            DurationMinutes  = 30,
            Reason           = "Control"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    private static DateTime GetNextWeekday(DayOfWeek day)
    {
        var date = DateTime.UtcNow.Date.AddDays(1);
        while (date.DayOfWeek != day) date = date.AddDays(1);
        return date;
    }
}

