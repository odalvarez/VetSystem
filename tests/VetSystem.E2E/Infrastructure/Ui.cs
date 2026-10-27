using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace VetSystem.E2E.Infrastructure;

public static class Ui
{
    // Una carga completa vuelve a arrancar el runtime de WASM; en WebKit dentro del contenedor eso supera
    // a veces los 5 s del expect por defecto. El arranque tiene su propio presupuesto y los asserts conservan el suyo.
    private const float BootTimeoutMs = 30_000;

    public static async Task OpenAsync(IPage page, string path)
    {
        await page.GotoAsync(path);
        await WaitForBootAsync(page);
    }

    public static async Task ReloadAsync(IPage page)
    {
        await page.ReloadAsync();
        await WaitForBootAsync(page);
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<IPage, List<string>> PageEvents = new();

    // Guarda lo que el navegador reporta de cada página para poder explicar un arranque que no ocurre
    public static void Track(IPage page)
    {
        var events = PageEvents.GetOrCreateValue(page);
        page.Console       += (_, m) => { if (m.Type is "error" or "warning") events.Add($"console {m.Type}: {m.Text}"); };
        page.PageError     += (_, e) => events.Add($"pageerror: {e}");
        page.RequestFailed += (_, r) => events.Add($"requestfailed: {r.Url} {r.Failure}");
        page.Response      += (_, r) => { if (r.Status >= 400) events.Add($"http {r.Status}: {r.Url}"); };
        page.Crash         += (_, _) => events.Add("crash de la página");
    }

    private static async Task WaitForBootAsync(IPage page)
    {
        try
        {
            await Expect(page.Locator(".app-layout, .auth-wrapper").First).ToBeAttachedAsync(new() { Timeout = BootTimeoutMs });
        }
        catch (PlaywrightException ex)
        {
            var app    = await page.EvaluateAsync<string>("document.getElementById('app')?.innerHTML.slice(0, 200) ?? 'sin #app'");
            var events = PageEvents.TryGetValue(page, out var list) ? string.Join(" | ", list.Take(8)) : "sin seguimiento";
            throw new InvalidOperationException($"La app no arrancó en {BootTimeoutMs / 1000:0} s en {page.Url}. #app = {app}. Eventos: {events}", ex);
        }
    }

    public static async Task LoginAsync(IPage page, string email, string password)
    {
        await OpenAsync(page, "/login");
        await page.GetByLabel("Correo electrónico").FillAsync(email);
        await page.GetByLabel("Contraseña").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Ingresar" }).ClickAsync();
    }

    public static async Task LoginAsync(IPage page, TestUser user, string expectedPath)
    {
        await LoginAsync(page, user.Email, user.Password);
        try
        {
            await Expect(page).ToHaveURLAsync(new Regex(Regex.Escape(expectedPath) + "$"));
        }
        catch (PlaywrightException ex)
        {
            // Distingue un login rechazado (alerta visible) de uno que ni siquiera llegó a enviarse o se cayó
            var alerts = await page.Locator(".alert, .validation-message").AllInnerTextsAsync();
            var email  = await page.GetByLabel("Correo electrónico").InputValueAsync();
            var events = PageEvents.TryGetValue(page, out var list) ? string.Join(" | ", list.Take(8)) : "sin seguimiento";
            throw new InvalidOperationException(
                $"El login no llegó a {expectedPath} (URL {page.Url}). Alertas: [{string.Join("; ", alerts)}]. Correo en el campo: '{email}'. Eventos: {events}", ex);
        }
    }

    // Si un formulario no se cierra tras enviarlo, el fallo dice qué error mostró en vez de solo "visible"
    public static async Task ExpectSubmittedAsync(ILocator dialog)
    {
        try
        {
            await Expect(dialog).ToBeHiddenAsync();
        }
        catch (PlaywrightException ex)
        {
            var shown = await dialog.Locator(".alert, .validation-message").AllInnerTextsAsync();
            throw new InvalidOperationException($"El diálogo siguió abierto. Errores visibles: [{string.Join(" | ", shown)}]", ex);
        }
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
