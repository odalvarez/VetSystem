using System.Text.RegularExpressions;
using VetSystem.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace VetSystem.E2E.Regression;

[Collection(StackCollection.Name)]
public class SessionTests(StackFixture stack)
{
    [Fact]
    public async Task Logout_ReturnsToLogin_AndProtectsPages()
    {
        var page = await stack.NewPageAsync();
        await Ui.LoginAsync(page, stack.Vet, "/vet-dashboard");

        await Ui.Button(page, "Cerrar sesión").ClickAsync();
        await Expect(page).ToHaveURLAsync(new Regex("/login$"));

        await page.GotoAsync("/vet-dashboard");
        await Expect(page.GetByLabel("Correo electrónico")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Owner_CannotStayOnAdminPanel()
    {
        var owner = await stack.CreateUserAsync("Owner", "Felipe", "Castro");
        var page  = await stack.NewPageAsync();
        await Ui.LoginAsync(page, owner, "/owner-portal");

        await page.GotoAsync("/admin");

        await Expect(page).Not.ToHaveURLAsync(new Regex("/admin"));
        await Expect(page.GetByText("Panel de administración")).ToHaveCountAsync(0);
    }
}
