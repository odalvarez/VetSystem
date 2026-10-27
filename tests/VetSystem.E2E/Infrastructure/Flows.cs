using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace VetSystem.E2E.Infrastructure;

// Flujos de UI que se repiten entre roles; se escriben una vez para que un cambio de pantalla se corrija en un solo lugar
public static class Flows
{
    public static ILocator Dialog(IPage page, string name) =>
        page.GetByRole(AriaRole.Dialog, new() { Name = name });

    public static async Task RequestAppointmentAsync(IPage page, string petName, string vetName, string reason, int daysAhead)
    {
        var dialog = Dialog(page, "Solicitar cita");
        await Expect(dialog).ToBeVisibleAsync();

        var pet = dialog.GetByLabel("Mascota *");
        await pet.FillAsync(petName);
        await dialog.GetByText(petName).Last.ClickAsync();
        // Al elegir una sugerencia el campo pasa a "Nombre (Dueño)"
        await Expect(pet).ToHaveValueAsync(new Regex($@"^{Regex.Escape(petName)} \("));
        await dialog.GetByLabel("Veterinario *").SelectOptionAsync(new SelectOptionValue { Label = vetName });
        await dialog.GetByLabel("Fecha *").FillAsync(Ui.Workday(daysAhead));
        // Por texto y no por rol: la hora pasó de botón a radio con el rediseño
        await dialog.GetByText(new Regex(@"^\d{2}:\d{2}$")).First.ClickAsync();
        await dialog.GetByLabel("Motivo de la cita *").FillAsync(reason);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Solicitar cita", Exact = true }).ClickAsync();

        await Ui.ExpectSubmittedAsync(dialog);
    }

    // Elige la opción cuyo texto contiene el valor buscado: el formato exacto de la etiqueta puede cambiar
    public static async Task SelectOptionContainingAsync(ILocator select, string text)
    {
        var value = await select.Locator("option").Filter(new() { HasText = text }).First.GetAttributeAsync("value");
        await select.SelectOptionAsync(value!);
    }

    public static async Task FillPetFormAsync(ILocator dialog, string petName)
    {
        await dialog.GetByLabel("Nombre *").FillAsync(petName);
        await dialog.GetByLabel("Especie *").SelectOptionAsync(new SelectOptionValue { Label = "Perro" });
        await dialog.GetByLabel("Raza *").FillAsync("Golden Retriever");
        // El radio real está oculto visualmente; el usuario hace clic en su etiqueta
        await dialog.GetByText("Hembra", new() { Exact = true }).ClickAsync();
        await Expect(dialog.GetByRole(AriaRole.Radio, new() { Name = "Hembra" })).ToBeCheckedAsync();
        await dialog.GetByLabel("Edad *").FillAsync("4");
        await dialog.GetByLabel("Peso (kg) *").FillAsync("28.5");
    }
}
