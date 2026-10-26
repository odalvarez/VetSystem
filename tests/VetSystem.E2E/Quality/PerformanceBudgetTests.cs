using System.Text.Json;
using Microsoft.Playwright;
using VetSystem.E2E.Infrastructure;

namespace VetSystem.E2E.Quality;

public sealed record PageMetrics(double TransferKb, int Requests, double LcpMs);

// Presupuesto de rendimiento: el rediseño no puede empeorar más de un 10% lo medido antes de tocar la UI.
// Con E2E_RECORD_BASELINE=1 se graba la línea base; sin ella, se compara contra el archivo guardado.
// Solo se corre en Chromium (filtro Category=Performance): LCP y CDP no están disponibles igual en los demás motores.
[Collection(StackCollection.Name)]
[Trait("Category", "Performance")]
public class PerformanceBudgetTests(StackFixture stack)
{
    private const int Runs = 5;
    private const double Tolerance = 1.10;
    private static readonly string BaselinePath =
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "perf-baseline.json");

    [Theory]
    [InlineData("login")]
    [InlineData("vet")]
    [InlineData("owner")]
    public async Task PageStaysWithinBudget(string scenario)
    {
        var samples = new List<PageMetrics>();
        for (var i = 0; i < Runs; i++)
            samples.Add(await MeasureAsync(scenario));

        var median = new PageMetrics(
            Median(samples.Select(s => s.TransferKb)),
            (int)Median(samples.Select(s => (double)s.Requests)),
            Median(samples.Select(s => s.LcpMs)));

        var baseline = File.Exists(BaselinePath)
            ? JsonSerializer.Deserialize<Dictionary<string, PageMetrics>>(File.ReadAllText(BaselinePath))!
            : [];

        if (Environment.GetEnvironmentVariable("E2E_RECORD_BASELINE") == "1")
        {
            baseline[scenario] = median;
            File.WriteAllText(BaselinePath, JsonSerializer.Serialize(baseline, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Assert.True(baseline.ContainsKey(scenario), $"No hay línea base para '{scenario}'; grabarla con E2E_RECORD_BASELINE=1.");
        var limit = baseline[scenario];
        Assert.True(median.TransferKb <= limit.TransferKb * Tolerance, $"{scenario}: {median.TransferKb:F0} KB > {limit.TransferKb:F0} KB +10%");
        Assert.True(median.Requests  <= limit.Requests * Tolerance,   $"{scenario}: {median.Requests} peticiones > {limit.Requests} +10%");
        Assert.True(median.LcpMs     <= limit.LcpMs * Tolerance,      $"{scenario}: LCP {median.LcpMs:F0} ms > {limit.LcpMs:F0} ms +10%");
    }

    private async Task<PageMetrics> MeasureAsync(string scenario)
    {
        var page = await stack.NewPageAsync();
        string target;
        switch (scenario)
        {
            case "vet":
                await Ui.LoginAsync(page, stack.Vet, "/vet-dashboard");
                target = "/vet-dashboard";
                break;
            case "owner":
                var owner = await stack.CreateUserAsync("Owner", "Medición", "Rendimiento");
                await Ui.LoginAsync(page, owner, "/owner-portal");
                target = "/owner-portal";
                break;
            default:
                target = "/login";
                break;
        }

        // Carga en frío de la página medida (caché desactivada vía CDP): así cada corrida mide lo mismo
        var cdp = await page.Context.NewCDPSessionAsync(page);
        await cdp.SendAsync("Network.enable");
        await cdp.SendAsync("Network.setCacheDisabled", new Dictionary<string, object> { ["cacheDisabled"] = true });
        await page.GotoAsync(target, new() { WaitUntil = WaitUntilState.NetworkIdle });

        var json = await page.EvaluateAsync<JsonElement>(@"() => new Promise(resolve => {
            let lcp = 0;
            new PerformanceObserver(list => { for (const e of list.getEntries()) lcp = Math.max(lcp, e.startTime); })
                .observe({ type: 'largest-contentful-paint', buffered: true });
            setTimeout(() => {
                const nav = performance.getEntriesByType('navigation')[0];
                const res = performance.getEntriesByType('resource');
                const bytes = res.reduce((a, r) => a + (r.transferSize || r.encodedBodySize || 0), nav ? (nav.transferSize || 0) : 0);
                resolve({ kb: bytes / 1024, requests: res.length + 1, lcp: lcp || (nav ? nav.domContentLoadedEventEnd : 0) });
            }, 500);
        })");

        await page.Context.CloseAsync();
        return new PageMetrics(json.GetProperty("kb").GetDouble(), json.GetProperty("requests").GetInt32(), json.GetProperty("lcp").GetDouble());
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(v => v).ToArray();
        return sorted[sorted.Length / 2];
    }
}
