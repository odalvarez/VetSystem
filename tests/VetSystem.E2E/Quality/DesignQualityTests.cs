using System.Text.RegularExpressions;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using VetSystem.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace VetSystem.E2E.Quality;

// Garantías del sistema visual que las pruebas funcionales no ven: accesibilidad, responsive, teclado,
// fuente propia y CSP sin violaciones
[Collection(StackCollection.Name)]
[Trait("Category", "Design")]
public class DesignQualityTests(StackFixture stack) : E2ETest(stack)
{
    public static IEnumerable<object[]> Screens() =>
        new[]
        {
            "login", "register",
            "owner-portal", "owner-pet-detail", "owner-notifications",
            "vet-patients", "vet-appointments", "vet-new-appointment", "vet-new-pet", "vet-notifications",
            "admin-users", "admin-patients", "admin-appointments", "admin-species", "admin-schedules", "admin-user-modal"
        }.Select(s => new object[] { s });

    private async Task<IPage> OpenScreenAsync(string screen, int width = 1440, int height = 900)
    {
        var page = await NewPageAsync(width, height);
        // Las animaciones de entrada (fundidos) hacen que axe mida el contraste a media opacidad
        await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        switch (screen)
        {
            case "login":
                await Ui.OpenAsync(page, "/login");
                break;
            case "register":
                await Ui.OpenAsync(page, "/register");
                break;
            case var s when s.StartsWith("owner"):
                var owner = await Stack.CreateUserAsync("Owner", "Camila", "Rojas");
                await Stack.Admin.CreatePatientAsync(owner.Id, owner.FullName, owner.Phone, $"Luna{Random.Shared.Next(1000, 9999)}");
                await Ui.LoginAsync(page, owner, "/owner-portal");
                if (s == "owner-pet-detail")
                {
                    await page.Locator(".patient-card").First.ClickAsync();
                    await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
                }
                if (s == "owner-notifications")
                {
                    await page.GetByRole(AriaRole.Link, new() { Name = "Notificaciones" }).ClickAsync();
                    await Expect(page.GetByText("Historial", new() { Exact = true })).ToBeVisibleAsync();
                }
                break;
            case var s when s.StartsWith("vet"):
                await Ui.LoginAsync(page, Stack.Vet, "/vet-dashboard");
                if (s == "vet-appointments" || s == "vet-new-appointment")
                    await page.GetByRole(AriaRole.Button, new() { Name = "Citas" }).ClickAsync();
                if (s == "vet-new-appointment")
                    await Ui.Button(page, "Nueva cita").ClickAsync();
                if (s == "vet-new-pet")
                    await Ui.Button(page, "Registrar mascota").ClickAsync();
                if (s == "vet-notifications")
                {
                    await page.GetByRole(AriaRole.Link, new() { Name = "Notificaciones" }).ClickAsync();
                    await Expect(page.GetByText("Enviar notificación manual")).ToBeVisibleAsync();
                }
                break;
            default:
                await Ui.LoginAsync(page, Stack.Env.AdminEmail, Stack.Env.AdminPassword);
                var tab = screen switch
                {
                    "admin-patients" => "Mascotas", "admin-appointments" => "Citas",
                    "admin-species" => "Especies", "admin-schedules" => "Horarios", _ => "Usuarios"
                };
                await page.GetByRole(AriaRole.Button, new() { Name = tab }).First.ClickAsync();
                if (screen == "admin-user-modal")
                    await Ui.Button(page, "Nuevo usuario").ClickAsync();
                break;
        }
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return page;
    }

    [Theory]
    [MemberData(nameof(Screens))]
    public async Task Screen_HasNoSeriousAccessibilityViolations(string screen)
    {
        var page    = await OpenScreenAsync(screen);
        var results = await page.RunAxe();

        var serious = results.Violations
            .Where(v => v.Impact is "serious" or "critical")
            .Select(v => $"{v.Id} ({v.Impact}): {v.Help} — {string.Join(" | ", v.Nodes.Take(3).Select(n => n.Html))}")
            .ToList();
        Assert.True(serious.Count == 0, $"{screen}:\n{string.Join("\n", serious)}");
    }

    [Theory]
    [InlineData("login", 390)]
    [InlineData("register", 390)]
    [InlineData("owner-portal", 390)]
    [InlineData("vet-patients", 390)]
    [InlineData("vet-appointments", 390)]
    [InlineData("vet-new-appointment", 390)]
    [InlineData("admin-users", 390)]
    [InlineData("admin-schedules", 390)]
    [InlineData("vet-appointments", 768)]
    [InlineData("admin-users", 768)]
    public async Task Screen_HasNoHorizontalScroll(string screen, int width)
    {
        var page = await OpenScreenAsync(screen, width, 844);

        var overflow = await page.EvaluateAsync<int>("document.documentElement.scrollWidth - document.documentElement.clientWidth");
        Assert.True(overflow <= 0, $"{screen} a {width}px desborda {overflow}px en horizontal");
    }

    [Fact]
    public async Task MobileMenu_OpensAndNavigates()
    {
        var page = await OpenScreenAsync("vet-patients", 390, 844);

        var open = page.GetByRole(AriaRole.Button, new() { Name = "Abrir menú" });
        await open.ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Notificaciones" }).ClickAsync();

        await Expect(page.GetByText("Enviar notificación manual")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Abrir menú" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Modal_KeepsFocusInsideAndClosesWithEscape()
    {
        var page   = await OpenScreenAsync("vet-new-appointment");
        var dialog = Flows.Dialog(page, "Solicitar cita");

        for (var i = 0; i < 25; i++)
        {
            await page.Keyboard.PressAsync("Tab");
            // Al pasar el último control el foco sale a la interfaz del navegador (activeElement = body): es válido.
            // Lo inválido es aterrizar en el contenido de fondo, que debe estar inert
            var onBackground = await page.EvaluateAsync<bool>(
                "document.activeElement !== document.body && !document.activeElement.closest('[role=dialog]')");
            Assert.False(onBackground, $"El foco llegó al contenido de fondo tras {i + 1} Tab");
        }

        await page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToBeHiddenAsync();
    }

    [Theory]
    [InlineData("vet-patients")]
    [InlineData("vet-appointments")]
    [InlineData("admin-users")]
    [InlineData("admin-species")]
    [InlineData("owner-portal")]
    public async Task EveryButton_HasAccessibleName(string screen)
    {
        var page = await OpenScreenAsync(screen);

        var unnamed = await page.EvaluateAsync<string[]>(@"() => [...document.querySelectorAll('button')]
            .filter(b => !b.closest('[inert]'))
            .filter(b => !(b.getAttribute('aria-label') || b.innerText || b.getAttribute('title') || '').trim())
            .map(b => b.outerHTML.slice(0, 120))");
        Assert.True(unnamed.Length == 0, $"Botones sin nombre en {screen}: {string.Join(" | ", unnamed)}");
    }

    [Fact]
    public async Task InterFont_IsServedFromOwnOriginAndApplied()
    {
        var page = await OpenScreenAsync("login");

        var loaded = await page.EvaluateAsync<bool>(@"async () => { await document.fonts.ready; return document.fonts.check('16px Inter'); }");
        Assert.True(loaded, "Inter no se cargó");

        var external = await page.EvaluateAsync<string[]>(@"() => performance.getEntriesByType('resource')
            .map(r => r.name).filter(n => !n.startsWith(location.origin))");
        Assert.Empty(external);
    }

    [Fact]
    public async Task Pages_ProduceNoCspViolations()
    {
        var violations = new List<string>();
        var page = await NewPageAsync();
        page.Console += (_, m) =>
        {
            if (Regex.IsMatch(m.Text, "Content Security Policy|Refused to", RegexOptions.IgnoreCase))
                violations.Add(m.Text);
        };

        await Ui.LoginAsync(page, Stack.Vet, "/vet-dashboard");
        await page.GetByRole(AriaRole.Button, new() { Name = "Citas" }).ClickAsync();
        await Ui.Button(page, "Nueva cita").ClickAsync();
        await Expect(Flows.Dialog(page, "Solicitar cita")).ToBeVisibleAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.Empty(violations);
    }

    [Fact]
    public async Task Splash_IsReplacedByTheApp_UnderStrictCsp()
    {
        var page = await NewPageAsync();
        await page.GotoAsync("/login");

        await Expect(page.GetByLabel("Correo electrónico")).ToBeVisibleAsync();
        await Expect(page.Locator(".splash")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task PasswordRules_UpdateWhileTyping()
    {
        var page  = await OpenScreenAsync("register");
        var rules = page.GetByRole(AriaRole.List, new() { Name = "Requisitos de la contraseña" });
        var field = page.GetByLabel("Contraseña *", new() { Exact = true });

        await Expect(rules.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(4);
        await Expect(rules).ToContainTextAsync("Mínimo 8 caracteres (pendiente)");

        await field.FillAsync("Abcdefg1!");
        await Expect(rules).Not.ToContainTextAsync("(pendiente)");

        await field.FillAsync("abc");
        await Expect(rules.Locator(".met")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task PasswordRules_WarnWhenPasswordExceeds72Bytes()
    {
        var page = await OpenScreenAsync("register");

        await page.GetByLabel("Contraseña *", new() { Exact = true }).FillAsync("Aa1!" + new string('á', 40));

        await Expect(page.GetByText("Máximo 72 caracteres")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task OwnerPetDetail_ShowsReadOnlySheet_WithoutFormControls()
    {
        var page   = await OpenScreenAsync("owner-pet-detail");
        var dialog = page.GetByRole(AriaRole.Dialog);

        await Expect(dialog.GetByText("Fecha de nacimiento aproximada")).ToBeVisibleAsync();
        await Expect(dialog.GetByRole(AriaRole.Textbox)).ToHaveCountAsync(0);
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Guardar cambios" })).ToHaveCountAsync(0);
    }
}
