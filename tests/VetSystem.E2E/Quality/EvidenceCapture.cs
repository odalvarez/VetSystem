using Microsoft.Playwright;
using VetSystem.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace VetSystem.E2E.Quality;

// Capturas de cada pantalla para la evidencia antes/después del rediseño (tesis).
// Solo genera archivos si E2E_EVIDENCE_DIR está definida; en una corrida normal no hace nada.
[Collection(StackCollection.Name)]
[Trait("Category", "Evidence")]
public class EvidenceCapture(StackFixture stack) : E2ETest(stack)
{
    private static readonly (int Width, int Height, string Suffix)[] Viewports = [(1440, 900, "desktop"), (390, 844, "mobile")];

    [Fact]
    public async Task CaptureAllScreens()
    {
        var dir = Environment.GetEnvironmentVariable("E2E_EVIDENCE_DIR");
        if (string.IsNullOrEmpty(dir)) return;
        Directory.CreateDirectory(dir);

        var owner   = await Stack.CreateUserAsync("Owner", "Camila", "Rojas");
        var petName = "Luna";
        await Stack.Admin.CreatePatientAsync(owner.Id, owner.FullName, owner.Phone, petName);

        foreach (var (width, height, suffix) in Viewports)
        {
            async Task Shot(IPage page, string name) =>
                await page.ScreenshotAsync(new() { Path = Path.Combine(dir, $"{name}-{suffix}.png"), FullPage = true });

            var pub = await NewPageAsync(width, height);
            await Ui.OpenAsync(pub, "/login");
            await Expect(pub.GetByLabel("Correo electrónico")).ToBeVisibleAsync();
            await Shot(pub, "01-login");
            await Ui.OpenAsync(pub, "/register");
            await Expect(pub.GetByLabel("Nombre *")).ToBeVisibleAsync();
            await Shot(pub, "02-registro");

            var own = await NewPageAsync(width, height);
            await Ui.LoginAsync(own, owner, "/owner-portal");
            await Expect(own.GetByText(petName)).ToBeVisibleAsync();
            await Shot(own, "10-propietario-portal");
            await own.GetByText(petName).First.ClickAsync();
            await Expect(Flows.Dialog(own, petName)).ToBeVisibleAsync();
            await Shot(own, "11-propietario-detalle-mascota");
            await Ui.OpenAsync(own, "/notifications");
            await Expect(own.GetByText("Historial", new() { Exact = true })).ToBeVisibleAsync();
            await Shot(own, "12-propietario-notificaciones");

            var vet = await NewPageAsync(width, height);
            await Ui.LoginAsync(vet, Stack.Vet, "/vet-dashboard");
            await Shot(vet, "20-veterinario-pacientes");
            await vet.GetByRole(AriaRole.Button, new() { Name = "Citas" }).ClickAsync();
            await Shot(vet, "21-veterinario-citas");
            await Ui.Button(vet, "Nueva cita").ClickAsync();
            await Expect(Flows.Dialog(vet, "Solicitar cita")).ToBeVisibleAsync();
            await Shot(vet, "22-veterinario-modal-cita");
            await Ui.OpenAsync(vet, "/notifications");
            await Expect(vet.GetByText("Enviar notificación manual")).ToBeVisibleAsync();
            await Shot(vet, "23-veterinario-notificaciones");

            var admin = await NewPageAsync(width, height);
            await Ui.LoginAsync(admin, Stack.Env.AdminEmail, Stack.Env.AdminPassword);
            foreach (var (tab, n) in new[] { ("Usuarios", 30), ("Mascotas", 31), ("Citas", 32), ("Especies", 33), ("Horarios", 34) })
            {
                await admin.GetByRole(AriaRole.Button, new() { Name = tab }).First.ClickAsync();
                await admin.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Shot(admin, $"{n}-admin-{tab.ToLowerInvariant()}");
            }
            await admin.GetByRole(AriaRole.Button, new() { Name = "Usuarios" }).First.ClickAsync();
            await Ui.Button(admin, "Nuevo usuario").ClickAsync();
            await Expect(Flows.Dialog(admin, "Nuevo usuario")).ToBeVisibleAsync();
            await Shot(admin, "35-admin-modal-usuario");

            foreach (var p in new[] { pub, own, vet, admin })
                await p.Context.CloseAsync();
        }
    }
}
