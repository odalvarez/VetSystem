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
        // El InternalKeyMiddleware exige este header en todas las rutas que no sean /health o /swagger
        client.DefaultRequestHeaders.Add("X-Internal-Key", NotificationsWebFactory.InternalKey);
        return client;
    }

    // ── InternalKeyMiddleware ─────────────────────────────────────────────────

    [Fact]
    public async Task Request_WithoutInternalKey_Returns403()
    {
        var client = _factory.CreateClient();
        // Sin X-Internal-Key ni JWT
        var resp = await client.GetAsync("/api/notifications");
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
        var client = ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp   = await client.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ListNotifications_AsAdmin_Returns200()
    {
        var client = ClientAs(NotificationsWebFactory.AdminId, "admin@test.com", "Admin");
        var resp   = await client.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ListNotifications_AsOwner_Returns200()
    {
        var client = ClientAs(NotificationsWebFactory.OwnerId, "owner@test.com", "Owner");
        var resp   = await client.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Enviar WhatsApp ───────────────────────────────────────────────────────

    [Fact]
    public async Task SendWhatsApp_AsVet_Returns202()
    {
        var client = ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian");

        var resp = await client.PostAsJsonAsync("/api/notifications/whatsapp", new SendWhatsAppRequest
        {
            To      = "573001234567",
            Message = "Recordatorio de cita mañana a las 10am."
        });

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    }

    [Fact]
    public async Task SendWhatsApp_AsAdmin_Returns202()
    {
        var client = ClientAs(NotificationsWebFactory.AdminId, "admin@test.com", "Admin");

        var resp = await client.PostAsJsonAsync("/api/notifications/whatsapp", new SendWhatsAppRequest
        {
            To      = "573009876543",
            Message = "Mensaje de prueba del administrador."
        });

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    }

    // ── Enviar email ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SendEmail_AsVet_Returns202()
    {
        var client = ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian");

        var resp = await client.PostAsJsonAsync("/api/notifications/email", new SendEmailRequest
        {
            To      = "paciente@test.com",
            Subject = "Recordatorio de cita",
            Body    = "<p>Su cita es mañana a las 10am.</p>"
        });

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    }

    // ── Programar recordatorio ────────────────────────────────────────────────

    [Fact]
    public async Task ScheduleReminder_AsVet_Returns202()
    {
        var client = ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian");

        var resp = await client.PostAsJsonAsync("/api/notifications/reminder", new ScheduleReminderRequest
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
        var client = ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian");

        var resp = await client.PostAsJsonAsync("/api/notifications/reminder", new ScheduleReminderRequest
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
}
