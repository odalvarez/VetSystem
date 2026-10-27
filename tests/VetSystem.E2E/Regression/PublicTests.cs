using System.Text.RegularExpressions;
using VetSystem.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace VetSystem.E2E.Regression;

[Collection(StackCollection.Name)]
public class PublicTests(StackFixture stack) : E2ETest(stack)
{
    [Fact]
    public async Task Login_WithValidCredentials_GoesToRoleHome()
    {
        var page = await NewPageAsync();

        await Ui.LoginAsync(page, Stack.Vet, "/vet-dashboard");

        await Expect(page.GetByText(Stack.Vet.FullName)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShowsErrorAndStays()
    {
        var page = await NewPageAsync();

        await Ui.LoginAsync(page, Stack.Vet.Email, "Incorrecta#2026");

        await Expect(page.GetByText("Correo o contraseña incorrectos.")).ToBeVisibleAsync();
        await Expect(page).ToHaveURLAsync(new Regex("/login$"));
    }

    [Fact]
    public async Task Register_NewOwner_ThenCanLogin()
    {
        var page  = await NewPageAsync();
        var email = $"e2e-{Guid.NewGuid():N}@example.com";

        await Ui.OpenAsync(page, "/register");
        await page.GetByLabel("Nombre *").FillAsync("Camila");
        await page.GetByLabel("Apellido *").FillAsync("Rojas");
        await page.GetByLabel("Correo electrónico *").FillAsync(email);
        await page.GetByLabel("Teléfono").FillAsync("3001234567");
        await page.GetByLabel("Contraseña *", new() { Exact = true }).FillAsync(StackFixture.Password);
        await page.GetByLabel("Confirmar contraseña *").FillAsync(StackFixture.Password);
        await Ui.Button(page, "Crear cuenta").ClickAsync();

        await Expect(page.GetByText("¡Cuenta creada con éxito!")).ToBeVisibleAsync();

        await Ui.LoginAsync(page, email, StackFixture.Password);
        await Expect(page).ToHaveURLAsync(new Regex("/owner-portal$"));
    }

    [Fact]
    public async Task Register_WithExistingEmail_ShowsError()
    {
        var page = await NewPageAsync();

        await Ui.OpenAsync(page, "/register");
        await page.GetByLabel("Nombre *").FillAsync("Otra");
        await page.GetByLabel("Apellido *").FillAsync("Persona");
        await page.GetByLabel("Correo electrónico *").FillAsync(Stack.Vet.Email);
        await page.GetByLabel("Contraseña *", new() { Exact = true }).FillAsync(StackFixture.Password);
        await page.GetByLabel("Confirmar contraseña *").FillAsync(StackFixture.Password);
        await Ui.Button(page, "Crear cuenta").ClickAsync();

        await Expect(page.GetByText("Ya existe una cuenta con ese correo.")).ToBeVisibleAsync();
        await Expect(page.GetByText("¡Cuenta creada con éxito!")).Not.ToBeVisibleAsync();
    }
}
