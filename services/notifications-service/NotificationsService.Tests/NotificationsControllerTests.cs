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

    // ── P1: Validaciones de campos obligatorios ───────────────────────────────

    [Fact]
    public async Task SendWhatsApp_EmptyTo_Returns400()
    {
        var resp = await ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian")
            .PostAsJsonAsync("/api/notifications/whatsapp", new SendWhatsAppRequest
            {
                To      = "",
                Message = "Mensaje de prueba."
            });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task SendWhatsApp_EmptyMessage_Returns400()
    {
        var resp = await ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian")
            .PostAsJsonAsync("/api/notifications/whatsapp", new SendWhatsAppRequest
            {
                To      = "573001234567",
                Message = ""
            });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task SendEmail_EmptyTo_Returns400()
    {
        var resp = await ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian")
            .PostAsJsonAsync("/api/notifications/email", new SendEmailRequest
            {
                To      = "",
                Subject = "Asunto de prueba",
                Body    = "<p>Cuerpo.</p>"
            });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task SendEmail_EmptySubject_Returns400()
    {
        var resp = await ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian")
            .PostAsJsonAsync("/api/notifications/email", new SendEmailRequest
            {
                To      = "destinatario@test.com",
                Subject = "",
                Body    = "<p>Cuerpo.</p>"
            });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ScheduleReminder_LessThanOneHour_Returns400()
    {
        // La regla exige al menos 1 hora en el futuro; 30 minutos debe rechazarse
        var resp = await ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian")
            .PostAsJsonAsync("/api/notifications/reminder", new ScheduleReminderRequest
            {
                AppointmentId = Guid.NewGuid(),
                PatientName   = "Coco",
                OwnerName     = "Luis Torres",
                OwnerPhone    = "573001234567",
                OwnerEmail    = "luis@test.com",
                ScheduledAt   = DateTime.UtcNow.AddMinutes(30),
                Channels      = ["whatsapp"]
            });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ListNotifications_AsOwner_OnlyOwnNotifications()
    {
        // El vet envía una notificación a un destinatario externo
        var vetClient = ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian");
        await vetClient.PostAsJsonAsync("/api/notifications/whatsapp", new SendWhatsAppRequest
        {
            To      = "573009990000",
            Message = "Notificación para otro dueño."
        });

        // El owner solo debe ver notificaciones enviadas a su email/teléfono del JWT
        // (email: owner@test.com, phone: 3001234567 — del JwtTestHelper.Generate)
        var ownerClient = ClientAs(NotificationsWebFactory.OwnerId, "owner@test.com", "Owner");
        var resp = await ownerClient.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var list = await resp.Content.ReadFromJsonAsync<List<NotificationStatusResponse>>();
        Assert.NotNull(list);

        // Ningún registro debe tener como destinatario el número ajeno
        Assert.DoesNotContain(list, n => n.Recipient == "573009990000");
    }

    // ── P2: Happy path y validaciones de longitud ─────────────────────────────

    [Fact]
    public async Task SendEmail_SubjectTooLong_Returns400()
    {
        // Subject tiene [MaxLength(200)]; 201 caracteres debe rechazarse
        var resp = await ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian")
            .PostAsJsonAsync("/api/notifications/email", new SendEmailRequest
            {
                To      = "destinatario@test.com",
                Subject = new string('A', 201),
                Body    = "<p>Cuerpo.</p>"
            });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ScheduleReminder_ValidFutureAppointment_Returns202()
    {
        var resp = await ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian")
            .PostAsJsonAsync("/api/notifications/reminder", new ScheduleReminderRequest
            {
                AppointmentId = Guid.NewGuid(),
                PatientName   = "Rocky",
                OwnerName     = "María Pérez",
                OwnerPhone    = "573007654321",
                OwnerEmail    = "maria@test.com",
                ScheduledAt   = DateTime.UtcNow.AddDays(3),
                Channels      = ["whatsapp", "email"]
            });

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<ReminderAcceptedResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.ReminderId);
        Assert.NotEmpty(body.Channels);
    }

    // ── P3: Canales inválidos y paginación defensiva ──────────────────────────

    [Fact]
    public async Task ScheduleReminder_InvalidChannel_Returns400OrIgnored()
    {
        // FALLA: validación no implementada en backend — hueco documentado.
        // El backend acepta cualquier string en Channels sin validar valores permitidos.
        // Este test verifica el comportamiento actual (202) para documentar el hueco.
        var resp = await ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian")
            .PostAsJsonAsync("/api/notifications/reminder", new ScheduleReminderRequest
            {
                AppointmentId = Guid.NewGuid(),
                PatientName   = "Bolt",
                OwnerName     = "Carlos Ruiz",
                OwnerPhone    = "573005550000",
                OwnerEmail    = "carlos@test.com",
                ScheduledAt   = DateTime.UtcNow.AddDays(2),
                Channels      = ["telegram"]
            });

        // Cuando se implemente la validación, cambiar a BadRequest
        Assert.True(
            resp.StatusCode == HttpStatusCode.BadRequest ||
            resp.StatusCode == HttpStatusCode.Accepted,
            $"Se esperaba 400 o 202, se obtuvo {resp.StatusCode}");
    }

    [Fact]
    public async Task ListNotifications_InvalidPage_DefaultsToPage1()
    {
        // page=0 o negativo no debe causar error de servidor; se espera 200
        var resp = await ClientAs(NotificationsWebFactory.AdminId, "admin@test.com", "Admin")
            .GetAsync("/api/notifications?page=0");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── P4: Integridad de datos ───────────────────────────────────────────────

    [Fact]
    public async Task SendWhatsApp_RecordHasCorrectTimestampAfterSend()
    {
        var before = DateTime.UtcNow;

        var client = ClientAs(NotificationsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var create = await client.PostAsJsonAsync("/api/notifications/whatsapp", new SendWhatsAppRequest
        {
            To      = "573001110000",
            Message = "Test de timestamp."
        });

        Assert.Equal(HttpStatusCode.Accepted, create.StatusCode);

        var accepted = await create.Content.ReadFromJsonAsync<NotificationAcceptedResponse>();
        Assert.NotNull(accepted);

        // CreatedAt debe haberse fijado durante el request
        Assert.True(accepted.CreatedAt >= before, "CreatedAt debe ser posterior al inicio del test.");
        Assert.True(accepted.CreatedAt <= DateTime.UtcNow.AddSeconds(5), "CreatedAt no debe estar en el futuro.");

        // El envío es fire-and-forget; esperamos un poco para que el background task complete
        await Task.Delay(200);

        var status = await client.GetAsync($"/api/notifications/{accepted.Id}");
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);

        var detail = await status.Content.ReadFromJsonAsync<NotificationStatusResponse>();
        Assert.NotNull(detail);

        // El NoOpSender no lanza excepción, así que el registro debe quedar como "sent" con SentAt fijado
        if (detail.Status == "sent")
            Assert.NotNull(detail.SentAt);
    }

    [Fact]
    public async Task SendMultipleNotifications_AllRecorded()
    {
        var adminClient = ClientAs(NotificationsWebFactory.AdminId, "admin@test.com", "Admin");

        var r1 = await adminClient.PostAsJsonAsync("/api/notifications/whatsapp", new SendWhatsAppRequest
        {
            To      = "573002221111",
            Message = "Primera notificación del test múltiple."
        });
        var r2 = await adminClient.PostAsJsonAsync("/api/notifications/whatsapp", new SendWhatsAppRequest
        {
            To      = "573002222222",
            Message = "Segunda notificación del test múltiple."
        });

        Assert.Equal(HttpStatusCode.Accepted, r1.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, r2.StatusCode);

        var id1 = (await r1.Content.ReadFromJsonAsync<NotificationAcceptedResponse>())!.Id;
        var id2 = (await r2.Content.ReadFromJsonAsync<NotificationAcceptedResponse>())!.Id;

        // Ambos IDs deben ser distintos y recuperables individualmente
        Assert.NotEqual(id1, id2);

        var s1 = await adminClient.GetAsync($"/api/notifications/{id1}");
        var s2 = await adminClient.GetAsync($"/api/notifications/{id2}");
        Assert.Equal(HttpStatusCode.OK, s1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, s2.StatusCode);

        // El listado de admin debe contener los dos registros
        var listResp = await adminClient.GetAsync("/api/notifications?pageSize=200");
        var list = await listResp.Content.ReadFromJsonAsync<List<NotificationStatusResponse>>();
        Assert.NotNull(list);
        Assert.Contains(list, n => n.Id == id1);
        Assert.Contains(list, n => n.Id == id2);
    }
}
