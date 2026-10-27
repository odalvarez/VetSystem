using Microsoft.Playwright;

namespace VetSystem.E2E.Infrastructure;

// Cada test cierra los contextos que abrió: el navegador vive toda la corrida y, sin esto,
// las páginas acumuladas (sobre todo en WebKit) agotaban la memoria de la VM de Docker y tumbaban a SQL Server
public abstract class E2ETest(StackFixture stack) : IAsyncLifetime
{
    protected StackFixture Stack { get; } = stack;
    private readonly List<IBrowserContext> _contexts = [];

    protected async Task<IPage> NewPageAsync(int width = 1440, int height = 900)
    {
        var context = await Stack.NewContextAsync(width, height);
        _contexts.Add(context);
        var page = await context.NewPageAsync();
        Ui.Track(page);
        return page;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
            await context.CloseAsync();
    }
}
