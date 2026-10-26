using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace VetSystem.E2E.Infrastructure;

public static class Ui
{
    public static async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync("/login");
        await page.GetByLabel("Correo electrónico").FillAsync(email);
        await page.GetByLabel("Contraseña").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Ingresar" }).ClickAsync();
    }

    public static async Task LoginAsync(IPage page, TestUser user, string expectedPath)
    {
        await LoginAsync(page, user.Email, user.Password);
        await Expect(page).ToHaveURLAsync(new Regex(Regex.Escape(expectedPath) + "$"));
    }

    // El texto del botón cambia con el rediseño (iconos, "+"); se busca por coincidencia parcial
    public static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name });

    public static async Task SelectByLabelAsync(IPage page, string label, string optionText) =>
        await page.GetByLabel(label, new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = optionText });

    // Primer día hábil (lun–sáb) a partir de minDays, en el formato del input date
    public static string Workday(int minDays)
    {
        var d = DateTime.Today.AddDays(minDays);
        if (d.DayOfWeek == DayOfWeek.Sunday) d = d.AddDays(1);
        return d.ToString("yyyy-MM-dd");
    }
}
