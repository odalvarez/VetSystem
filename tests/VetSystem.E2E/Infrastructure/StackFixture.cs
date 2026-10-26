using Microsoft.Playwright;

namespace VetSystem.E2E.Infrastructure;

public sealed record TestUser(Guid Id, string FirstName, string LastName, string Email, string Password, string Phone)
{
    public string FullName => $"{FirstName} {LastName}";
}

// Un solo navegador y un solo veterinario por corrida; cada test crea sus propios datos
// para no depender del orden de ejecución
public sealed class StackFixture : IAsyncLifetime
{
    public const string Password    = "E2ePassw0rd!";
    public const string VaccineName = "Antirrábica";

    public E2EEnvironment Env { get; } = new();
    public ApiClient Admin { get; private set; } = null!;
    public TestUser Vet { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;
    private IPlaywright _playwright = null!;

    public async Task InitializeAsync()
    {
        Admin = new ApiClient(Env.BaseUrl);
        await Admin.LoginAsync(Env.AdminEmail, Env.AdminPassword);
        await Admin.SetClinicHoursAsync("08:00", "18:00",
            "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday");
        await Admin.EnsureVaccineAsync(VaccineName);
        Vet = await CreateUserAsync("Veterinarian", "Mariana", "López");

        _playwright = await Playwright.CreateAsync();
        var type = Env.Browser switch
        {
            "firefox" => _playwright.Firefox,
            "webkit"  => _playwright.Webkit,
            _         => _playwright.Chromium
        };
        Browser = await type.LaunchAsync();
    }

    public async Task<TestUser> CreateUserAsync(string role, string firstName, string lastName)
    {
        var email = $"e2e-{Guid.NewGuid():N}@example.com";
        var phone = "573001234567";
        var id = await Admin.CreateUserAsync(firstName, lastName, email, Password, phone, role);
        return new TestUser(id, firstName, lastName, email, Password, phone);
    }

    public async Task<IPage> NewPageAsync(int width = 1440, int height = 900)
    {
        var context = await Browser.NewContextAsync(new()
        {
            IgnoreHTTPSErrors = true,
            BaseURL           = Env.BaseUrl,
            ViewportSize      = new() { Width = width, Height = height },
            Locale            = "es-CO"
        });
        return await context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await Browser.CloseAsync();
        _playwright.Dispose();
        Admin.Dispose();
    }
}

[CollectionDefinition(Name)]
public sealed class StackCollection : ICollectionFixture<StackFixture>
{
    public const string Name = "Stack E2E";
}
