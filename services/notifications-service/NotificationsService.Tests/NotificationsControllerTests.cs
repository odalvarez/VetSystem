using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NotificationsService.Application.DTOs;
using NotificationsService.Tests.Helpers;

namespace NotificationsService.Tests;

public class NotificationsControllerTests : IClassFixture<NotificationsWebFactory>
{
    private readonly NotificationsWebFactory _factory;

    public NotificationsControllerTests(NotificationsWebFactory factory) => _factory = factory;

    private HttpClient ClientAs(Guid userId, string email, string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                JwtTestHelper.Generate(userId, email, role));
        client.DefaultRequestHeaders.Add("X-Internal-Key", NotificationsWebFactory.InternalKey);
        return client;
    }

    // ── InternalKeyMiddleware ─────────────────────────────────────────────────

    [Fact]
    public async Task Request_WithoutInternalKey_Returns403()
    {
        var resp = await _factory.CreateClient().GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Request_WithWrongInternalKey_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Internal-Key", "wrong-key");
        var resp = await client.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── Auth guard ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListNotifications_WithKeyButNoJwt_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Internal-Key", NotificationsWebFactory.InternalKey);
        var resp = await client.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Listar notificaciones ─────────────────────────────────────────────────

    [Fact]
    public async Task ListNotifications_AsVet_Returns200()
    {
        var resp = await ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian")
            .GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ListNotifications_AsAdmin_Returns200()
    {
        var resp = await ClientAs(NotificationsWebFactory.AdminId, "admin@test.com", "Admin")
            .GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ListNotifications_AsOwner_Returns200()
    {
        var resp = await ClientAs(NotificationsWebFactory.OwnerId, "owner@test.com", "Owner")
            .GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Enviar WhatsApp ───────────────────────────────────────────────────────

    [Fact]
    public async Task SendWhatsApp_AsVet_Returns202()
    {
        var resp = await ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian")
            .PostAsJsonAsync("/api/notifications/whatsapp", new SendWhatsAppRequest
            {
                To      = "573001234567",
                Message = "Recordatorio de cita mañana a las 10am."
            });

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    }

    [Fact]
    public async Task SendWhatsApp_AsAdmin_Returns202()
    {
        var resp = await ClientAs(NotificationsWebFactory.AdminId, "admin@test.com", "Admin")
            .PostAsJsonAsync("/api/notifications/whatsapp", new SendWhatsAppRequest
            {
                To      = "573009876543",
                Message = "Mensaje de prueba del administrador."
            });

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    }

    [Fact]
    public async Task SendWhatsApp_AsOwner_Returns202()
    {
        var resp = await ClientAs(NotificationsWebFactory.OwnerId, "owner@test.com", "Owner")
            .PostAsJsonAsync("/api/notifications/whatsapp", new SendWhatsAppRequest
            {
                To      = "573001112233",
                Message = "Mensaje de owner."
            });

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    }

    // ── Enviar email ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SendEmail_AsVet_Returns202()
    {
        var resp = await ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian")
            .PostAsJsonAsync("/api/notifications/email", new SendEmailRequest
            {
                To      = "paciente@test.com",
                Subject = "Recordatorio de cita",
                Body    = "<p>Su cita es mañana a las 10am.</p>"
            });

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    }

    [Fact]
    public async Task SendEmail_AsAdmin_Returns202()
    {
        var resp = await ClientAs(NotificationsWebFactory.AdminId, "admin@test.com", "Admin")
            .PostAsJsonAsync("/api/notifications/email", new SendEmailRequest
            {
                To      = "admin-destino@test.com",
                Subject = "Prueba admin",
                Body    = "<p>Correo de prueba.</p>"
            });

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    }

    // ── Programar recordatorio ────────────────────────────────────────────────

    [Fact]
    public async Task ScheduleReminder_AsVet_Returns202()
    {
        var resp = await ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian")
            .PostAsJsonAsync("/api/notifications/reminder", new ScheduleReminderRequest
            {
                AppointmentId = Guid.NewGuid(),
                PatientName   = "Luna",
                OwnerName     = "Pedro Ramirez",
                OwnerPhone    = "573001234567",
                OwnerEmail    = "pedro@test.com",
                ScheduledAt   = DateTime.UtcNow.AddDays(2),
                Channels      = ["whatsapp", "email"]
            });

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    }

    [Fact]
    public async Task ScheduleReminder_InThePast_Returns400()
    {
        var resp = await ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian")
            .PostAsJsonAsync("/api/notifications/reminder", new ScheduleReminderRequest
            {
                AppointmentId = Guid.NewGuid(),
                PatientName   = "Max",
                OwnerName     = "Ana García",
                OwnerPhone    = "573009876543",
                OwnerEmail    = "ana@test.com",
                ScheduledAt   = DateTime.UtcNow.AddDays(-1),
                Channels      = ["email"]
            });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Obtener notificación por ID ───────────────────────────────────────────

    [Fact]
    public async Task GetNotification_AsVet_Returns200()
    {
        var client = ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var create = await client.PostAsJsonAsync("/api/notifications/whatsapp", new SendWhatsAppRequest
        {
            To      = "573001230000",
            Message = "Para obtener por ID."
        });
        var notification = await create.Content.ReadFromJsonAsync<NotificationAcceptedResponse>();

        var resp = await client.GetAsync($"/api/notifications/{notification!.Id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetNotification_Unauthenticated_Returns403()
    {
        // Sin X-Internal-Key el middleware rechaza antes de llegar al JWT
        var resp = await _factory.CreateClient().GetAsync($"/api/notifications/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }
}
