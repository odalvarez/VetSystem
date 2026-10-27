using Microsoft.Playwright;
using VetSystem.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace VetSystem.E2E.Regression;

[Collection(StackCollection.Name)]
public class NotificationTests(StackFixture stack) : E2ETest(stack)
{
    // WireMock responde 500 a los números terminados en 3009999999 para simular un fallo de Evolution API
    private const string FailingLocalNumber = "3009999999";

    private async Task<IPage> NotificationsPageAsync()
    {
        var page = await NewPageAsync();
        await Ui.LoginAsync(page, Stack.Vet, "/vet-dashboard");
        await page.GetByRole(AriaRole.Link, new() { Name = "Notificaciones" }).ClickAsync();
        await Expect(page.GetByText("Enviar notificación manual")).ToBeVisibleAsync();
        return page;
    }

    private static async Task SendWhatsAppAsync(IPage page, string localNumber, string message)
    {
        await page.GetByRole(AriaRole.Group, new() { Name = "Tipo" }).GetByText("WhatsApp").ClickAsync();
        await page.GetByLabel("Número WhatsApp").FillAsync(localNumber);
        await page.GetByLabel("Mensaje", new() { Exact = true }).FillAsync(message);
        await Ui.Button(page, "Enviar WhatsApp").ClickAsync();
    }

    [Fact]
    public async Task ManualWhatsApp_IsDeliveredAndListedAsSent()
    {
        var page    = await NotificationsPageAsync();
        var local   = $"300{Random.Shared.Next(1000000, 8999999)}";
        var message = $"Recordatorio E2E {Guid.NewGuid():N}";

        await SendWhatsAppAsync(page, local, message);

        using var doubles = new MessageDoubles(Stack.Env);
        var calls = await MessageDoubles.EventuallyAsync(() => doubles.WhatsAppCallsToAsync($"57{local}"), c => c > 0);
        Assert.True(calls > 0, "WireMock no recibió el WhatsApp manual");

        var row = page.GetByRole(AriaRole.Row).Filter(new() { HasText = local });
        await Expect(row).ToContainTextAsync("Enviada");
    }

    [Fact]
    public async Task ManualWhatsApp_ProviderFailure_IsListedAsFailed_WithDetail()
    {
        var page = await NotificationsPageAsync();

        await SendWhatsAppAsync(page, FailingLocalNumber, $"Prueba de fallo {Guid.NewGuid():N}");

        var row = page.GetByRole(AriaRole.Row).Filter(new() { HasText = FailingLocalNumber }).First;
        await Expect(row).ToContainTextAsync("Fallida");

        await row.GetByRole(AriaRole.Button, new() { Name = "Ver detalle" }).ClickAsync();
        var detail = Flows.Dialog(page, "Detalle de notificación");
        await Expect(detail).ToBeVisibleAsync();
        await Expect(detail).ToContainTextAsync(FailingLocalNumber);
    }
}
