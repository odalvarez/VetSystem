using Microsoft.Playwright;
using VetSystem.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace VetSystem.E2E.Regression;

[Collection(StackCollection.Name)]
public class OwnerTests(StackFixture stack)
{
    private async Task<(IPage Page, TestUser Owner)> OwnerSessionAsync()
    {
        var owner = await stack.CreateUserAsync("Owner", "Camila", "Rojas");
        var page  = await stack.NewPageAsync();
        await Ui.LoginAsync(page, owner, "/owner-portal");
        return (page, owner);
    }

    [Fact]
    public async Task RegisterPet_ShowsItInMyPets()
    {
        var (page, _) = await OwnerSessionAsync();
        var petName   = $"Luna{Random.Shared.Next(1000, 9999)}";

        await Ui.Button(page, "Registrar mascota").First.ClickAsync();
        var dialog = Flows.Dialog(page, "Registrar nueva mascota");
        await Flows.FillPetFormAsync(dialog, petName);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Registrar mascota" }).ClickAsync();

        await Expect(dialog).ToBeHiddenAsync();
        await Expect(page.GetByText(petName)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task RequestAppointment_AppearsInMyAppointments_AndNotifiesOwner()
    {
        var (page, owner) = await OwnerSessionAsync();
        var petName       = $"Milo{Random.Shared.Next(1000, 9999)}";
        await stack.Admin.CreatePatientAsync(owner.Id, owner.FullName, owner.Phone, petName);
        await page.ReloadAsync();

        await Ui.Button(page, "Solicitar cita").ClickAsync();
        await Flows.RequestAppointmentAsync(page, petName, stack.Vet.FullName, "Control anual", daysAhead: 9);

        var row = page.GetByRole(AriaRole.Row).Filter(new() { HasText = petName });
        await Expect(row).ToContainTextAsync("Programada");
        await Expect(row).ToContainTextAsync("Control anual");

        using var doubles = new MessageDoubles(stack.Env);
        var emails = await MessageDoubles.EventuallyAsync(() => doubles.EmailsToAsync(owner.Email), m => m.Count > 0);
        Assert.NotEmpty(emails);
        var calls = await MessageDoubles.EventuallyAsync(() => doubles.WhatsAppCallsToAsync(owner.Phone), c => c > 0);
        Assert.True(calls > 0, "WireMock no recibió el WhatsApp de confirmación");
    }

    [Fact]
    public async Task PetDetail_IsReadOnlyForOwner()
    {
        var (page, owner) = await OwnerSessionAsync();
        var petName       = $"Simba{Random.Shared.Next(1000, 9999)}";
        await stack.Admin.CreatePatientAsync(owner.Id, owner.FullName, owner.Phone, petName, "cat");
        await page.ReloadAsync();

        await page.GetByText(petName).ClickAsync();
        var dialog = Flows.Dialog(page, petName);
        await Expect(dialog).ToBeVisibleAsync();

        foreach (var field in await dialog.GetByRole(AriaRole.Textbox).AllAsync())
            await Expect(field).ToBeDisabledAsync();
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Guardar cambios" })).ToHaveCountAsync(0);

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Vacunas" }).ClickAsync();
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Registrar dosis" })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Notifications_HasNoManualSendForOwner()
    {
        var (page, _) = await OwnerSessionAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "Notificaciones" }).ClickAsync();

        await Expect(page.GetByText("Historial", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByText("Enviar notificación manual")).ToHaveCountAsync(0);
    }
}
