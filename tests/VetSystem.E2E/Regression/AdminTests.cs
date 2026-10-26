using Microsoft.Playwright;
using VetSystem.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace VetSystem.E2E.Regression;

[Collection(StackCollection.Name)]
public class AdminTests(StackFixture stack)
{
    private async Task<IPage> AdminSessionAsync()
    {
        var page = await stack.NewPageAsync();
        await Ui.LoginAsync(page, stack.Env.AdminEmail, stack.Env.AdminPassword);
        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@"/admin(\?.*)?$"));
        return page;
    }

    private static ILocator UserRow(IPage page, string email) =>
        page.GetByRole(AriaRole.Row).Filter(new() { HasText = email });

    [Fact]
    public async Task UserLifecycle_CreateEditResetDeactivateDelete()
    {
        var page  = await AdminSessionAsync();
        var email = $"e2e-{Guid.NewGuid():N}@example.com";

        await Ui.Button(page, "Nuevo usuario").ClickAsync();
        var form = Flows.Dialog(page, "Nuevo usuario");
        await form.GetByLabel("Nombre *").FillAsync("Julián");
        await form.GetByLabel("Apellido *").FillAsync("Herrera");
        await form.GetByLabel("Correo electrónico *").FillAsync(email);
        await form.GetByLabel("Rol *").SelectOptionAsync(new SelectOptionValue { Label = "Veterinario" });
        await form.GetByLabel("Contraseña *", new() { Exact = true }).FillAsync(StackFixture.Password);
        await form.GetByLabel("Confirmar contraseña *").FillAsync(StackFixture.Password);
        await form.GetByRole(AriaRole.Button, new() { Name = "Crear usuario" }).ClickAsync();
        await Expect(form).ToBeHiddenAsync();

        await page.GetByLabel("Buscar usuarios").FillAsync(email);
        await Expect(UserRow(page, email)).ToContainTextAsync("Julián Herrera");

        await UserRow(page, email).GetByRole(AriaRole.Button, new() { Name = "Editar usuario" }).ClickAsync();
        var edit = Flows.Dialog(page, "Editar usuario");
        await edit.GetByLabel("Apellido *").FillAsync("Herrera Ruiz");
        await edit.GetByRole(AriaRole.Button, new() { Name = "Guardar cambios" }).ClickAsync();
        await Expect(UserRow(page, email)).ToContainTextAsync("Herrera Ruiz");

        const string newPassword = "Nueva#Clave2026";
        await UserRow(page, email).GetByRole(AriaRole.Button, new() { Name = "Resetear contraseña" }).ClickAsync();
        var reset = Flows.Dialog(page, "Resetear contraseña");
        await reset.GetByLabel("Nueva contraseña *").FillAsync(newPassword);
        await reset.GetByLabel("Confirmar *").FillAsync(newPassword);
        await reset.GetByRole(AriaRole.Button, new() { Name = "Confirmar", Exact = true }).ClickAsync();
        await Expect(reset).ToBeHiddenAsync();

        var userPage = await stack.NewPageAsync();
        await Ui.LoginAsync(userPage, email, newPassword);
        await Expect(userPage).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/vet-dashboard$"));

        await UserRow(page, email).GetByRole(AriaRole.Button, new() { Name = "Desactivar usuario" }).ClickAsync();
        await Expect(UserRow(page, email)).ToContainTextAsync("Inactivo");

        await UserRow(page, email).GetByRole(AriaRole.Button, new() { Name = "Eliminar usuario" }).ClickAsync();
        var delete = Flows.Dialog(page, "Eliminar usuario");
        await delete.GetByRole(AriaRole.Button, new() { Name = "Eliminar", Exact = true }).ClickAsync();
        await Expect(UserRow(page, email)).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Species_CreateEditDelete()
    {
        var page = await AdminSessionAsync();
        var name = $"Tortuga{Random.Shared.Next(1000, 9999)}";

        await page.GetByRole(AriaRole.Button, new() { Name = "Especies" }).ClickAsync();
        await Ui.Button(page, "Nueva especie").ClickAsync();
        await page.GetByLabel("Nombre de la nueva especie").FillAsync(name);
        await page.GetByRole(AriaRole.Button, new() { Name = "Guardar", Exact = true }).ClickAsync();

        var row = page.GetByRole(AriaRole.Row).Filter(new() { HasText = name });
        await Expect(row).ToBeVisibleAsync();

        await row.GetByRole(AriaRole.Button, new() { Name = "Editar especie" }).ClickAsync();
        await page.GetByLabel("Nombre de la especie", new() { Exact = true }).FillAsync($"{name}X");
        await page.GetByRole(AriaRole.Button, new() { Name = "Guardar especie" }).ClickAsync();
        var renamed = page.GetByRole(AriaRole.Row).Filter(new() { HasText = $"{name}X" });
        await Expect(renamed).ToBeVisibleAsync();

        await renamed.GetByRole(AriaRole.Button, new() { Name = "Eliminar especie" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Row).Filter(new() { HasText = $"{name}X" })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ClinicWorkdays_ArePersisted()
    {
        var page = await AdminSessionAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Horarios" }).ClickAsync();
        var clinic = page.GetByRole(AriaRole.Region, new() { Name = "Horario global de la clínica" });
        var sunday = clinic.GetByRole(AriaRole.Button, new() { Name = "Dom" });

        try
        {
            await Expect(sunday).ToHaveAttributeAsync("aria-pressed", "false");
            await sunday.ClickAsync();
            await clinic.GetByRole(AriaRole.Button, new() { Name = "Guardar", Exact = true }).ClickAsync();

            await page.ReloadAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Horarios" }).ClickAsync();
            await Expect(sunday).ToHaveAttributeAsync("aria-pressed", "true");
        }
        finally
        {
            // El resto de la suite asume que el domingo está cerrado
            await stack.Admin.SetClinicHoursAsync("08:00", "18:00",
                "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday");
        }
    }

    [Fact]
    public async Task VetLeave_IsRegistered()
    {
        var vet  = await stack.CreateUserAsync("Veterinarian", "Laura", $"Martínez{Random.Shared.Next(100, 999)}");
        var page = await AdminSessionAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Horarios" }).ClickAsync();

        var leaves = page.GetByRole(AriaRole.Region, new() { Name = "Ausencias y permisos" });
        await leaves.GetByLabel("Veterinario").SelectOptionAsync(new SelectOptionValue { Label = vet.FullName });
        await leaves.GetByLabel("Desde").FillAsync(Ui.Workday(30));
        await leaves.GetByLabel("Hasta").FillAsync(Ui.Workday(30));
        await leaves.GetByLabel("Motivo").FillAsync("Congreso veterinario");
        await leaves.GetByRole(AriaRole.Button, new() { Name = "Registrar ausencia" }).ClickAsync();

        await Expect(leaves.GetByText("Congreso veterinario")).ToBeVisibleAsync();
    }

    [Theory]
    [InlineData("Usuarios", "label",  "Buscar usuarios")]
    [InlineData("Mascotas", "label",  "Buscar mascotas")]
    [InlineData("Citas",    "label",  "Filtrar por estado")]
    [InlineData("Especies", "button", "Nueva especie")]
    [InlineData("Horarios", "label",  "Hora inicio")]
    public async Task EveryTab_ShowsItsOwnContent(string tab, string probeKind, string probeName)
    {
        var page = await AdminSessionAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = tab }).First.ClickAsync();

        var probe = probeKind == "button"
            ? page.GetByRole(AriaRole.Button, new() { Name = probeName })
            : page.GetByLabel(probeName).First;
        await Expect(probe).ToBeVisibleAsync();
    }
}
