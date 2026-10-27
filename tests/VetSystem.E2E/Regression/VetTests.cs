using System.Text.RegularExpressions;
using Microsoft.Playwright;
using VetSystem.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace VetSystem.E2E.Regression;

[Collection(StackCollection.Name)]
public class VetTests(StackFixture stack) : E2ETest(stack)
{
    private async Task<IPage> VetSessionAsync()
    {
        var page = await NewPageAsync();
        await Ui.LoginAsync(page, Stack.Vet, "/vet-dashboard");
        return page;
    }

    private async Task<(TestUser Owner, string PetName)> OwnerWithPetAsync(string prefix, string species = "dog")
    {
        var owner   = await Stack.CreateUserAsync("Owner", "Andrés", $"Gómez{Random.Shared.Next(100, 999)}");
        var petName = $"{prefix}{Random.Shared.Next(1000, 9999)}";
        await Stack.Admin.CreatePatientAsync(owner.Id, owner.FullName, owner.Phone, petName, species);
        return (owner, petName);
    }

    private static async Task<ILocator> OpenPetDetailAsync(IPage page, string petName)
    {
        await page.GetByLabel("Buscar mascotas").FillAsync(petName);
        await page.GetByText(petName).First.ClickAsync();
        var dialog = Flows.Dialog(page, petName);
        await Expect(dialog).ToBeVisibleAsync();
        return dialog;
    }

    [Fact]
    public async Task SearchAndFilterPatients()
    {
        var (_, dogName) = await OwnerWithPetAsync("Rocky");
        var (_, catName) = await OwnerWithPetAsync("Nala", "cat");
        var page = await VetSessionAsync();

        await page.GetByLabel("Buscar mascotas").FillAsync(dogName);
        await Expect(page.GetByText(dogName)).ToBeVisibleAsync();
        await Expect(page.GetByText(catName)).ToHaveCountAsync(0);

        await page.GetByLabel("Buscar mascotas").FillAsync(catName);
        await Flows.SelectOptionContainingAsync(page.GetByLabel("Filtrar por especie"), "Gato");
        await Expect(page.GetByText(catName)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task FilterPatientsByOwner()
    {
        var (owner, petName) = await OwnerWithPetAsync("Copito");
        var page = await VetSessionAsync();

        await page.GetByLabel("Buscar dueño").FillAsync(owner.LastName);
        await page.GetByText(owner.FullName).First.ClickAsync();

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Quitar filtro de dueño" })).ToBeVisibleAsync();
        await Expect(page.GetByText(petName)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task RegisterPetForOwner()
    {
        var owner   = await Stack.CreateUserAsync("Owner", "Valentina", $"Pérez{Random.Shared.Next(100, 999)}");
        var petName = $"Kiwi{Random.Shared.Next(1000, 9999)}";
        var page    = await VetSessionAsync();

        await Ui.Button(page, "Registrar mascota").ClickAsync();
        var dialog = Flows.Dialog(page, "Registrar mascota");
        await dialog.GetByLabel("Propietario *").FillAsync(owner.LastName);
        await dialog.GetByText(owner.FullName).First.ClickAsync();
        await Flows.FillPetFormAsync(dialog, petName);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Registrar mascota" }).ClickAsync();

        await Ui.ExpectSubmittedAsync(dialog);
        await page.GetByLabel("Buscar mascotas").FillAsync(petName);
        await Expect(page.GetByText(petName)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateLogAndCompleteAppointment()
    {
        var (_, petName) = await OwnerWithPetAsync("Toby");
        var page = await VetSessionAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Citas" }).ClickAsync();
        await Ui.Button(page, "Nueva cita").ClickAsync();
        await Flows.RequestAppointmentAsync(page, petName, Stack.Vet.FullName, "Vacunación", daysAhead: 12);

        var row = page.GetByRole(AriaRole.Row).Filter(new() { HasText = petName });
        await Expect(row).ToContainTextAsync("Programada");

        // Regla de negocio: la cita solo se completa con la bitácora creada y cerrada
        await row.GetByRole(AriaRole.Button, new() { Name = "Bitácora" }).ClickAsync();
        var log = Flows.Dialog(page, "Bitácora de consulta");
        await log.GetByLabel("Motivo de consulta *").FillAsync("Control posvacunación");
        await log.GetByLabel("Diagnóstico", new() { Exact = true }).FillAsync("Paciente sano");
        await log.GetByRole(AriaRole.Button, new() { Name = "Crear bitácora" }).ClickAsync();
        await log.GetByRole(AriaRole.Button, new() { Name = "Cerrar bitácora" }).ClickAsync();
        await Expect(log.GetByText("Bitácora cerrada correctamente.")).ToBeVisibleAsync();
        await log.GetByRole(AriaRole.Button, new() { Name = "No por ahora" }).ClickAsync();
        await Expect(log).ToBeHiddenAsync();

        await row.GetByRole(AriaRole.Button, new() { Name = "Completar" }).ClickAsync();
        await Expect(row).ToContainTextAsync("Completada");

        await page.GetByRole(AriaRole.Button, new() { Name = "Pacientes" }).ClickAsync();
        var detail = await OpenPetDetailAsync(page, petName);
        await detail.GetByRole(AriaRole.Button, new() { Name = "Bitácoras" }).ClickAsync();
        await Expect(detail.GetByText("Control posvacunación")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task EditPetData_IsPersisted()
    {
        var (_, petName) = await OwnerWithPetAsync("Bruno");
        var page = await VetSessionAsync();

        var dialog = await OpenPetDetailAsync(page, petName);
        await dialog.GetByLabel("Peso (kg) *").FillAsync("31.2");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Guardar cambios" }).ClickAsync();
        await Expect(page.GetByText("Mascota actualizada correctamente.")).ToBeVisibleAsync();

        await Ui.ReloadAsync(page);
        var reopened = await OpenPetDetailAsync(page, petName);
        await Expect(reopened.GetByLabel("Peso (kg) *")).ToHaveValueAsync(new Regex(@"^31[.,]2"));
    }

    [Fact]
    public async Task AddClinicalRecordEntry()
    {
        var (_, petName) = await OwnerWithPetAsync("Lola");
        var page = await VetSessionAsync();

        var dialog = await OpenPetDetailAsync(page, petName);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Historia clínica" }).ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Agregar entrada" }).ClickAsync();
        await dialog.GetByLabel("Motivo *").FillAsync("Otitis");
        await dialog.GetByLabel("Diagnóstico *").FillAsync("Otitis externa leve");
        await dialog.GetByLabel("Tratamiento *").FillAsync("Gotas óticas 7 días");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Guardar entrada" }).ClickAsync();

        await Expect(dialog.GetByText("Otitis externa leve")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task RegisterVaccination()
    {
        var (_, petName) = await OwnerWithPetAsync("Pelusa", "cat");
        var page = await VetSessionAsync();

        var dialog = await OpenPetDetailAsync(page, petName);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Vacunas" }).ClickAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Registrar dosis" }).ClickAsync();
        await Flows.SelectOptionContainingAsync(dialog.GetByLabel("Vacuna"), StackFixture.VaccineName);
        await dialog.GetByLabel("Fecha de aplicación").FillAsync(DateTime.Today.ToString("yyyy-MM-dd"));
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Guardar", Exact = true }).ClickAsync();

        await Expect(dialog.GetByText(StackFixture.VaccineName).Last).ToBeVisibleAsync();
        await Expect(dialog.GetByText("Dosis 1")).ToBeVisibleAsync();
    }
}
