namespace VetSystem.E2E.Infrastructure;

// Configuración de la corrida: variables de entorno primero y e2e.env como respaldo,
// para que la suite funcione igual en local y en CI sin duplicar valores
public sealed class E2EEnvironment
{
    public string BaseUrl       { get; }
    public string MailpitUrl    { get; }
    public string WireMockUrl   { get; }
    public string AdminEmail    { get; }
    public string AdminPassword { get; }
    public string Browser       { get; }

    public E2EEnvironment()
    {
        var file = LoadEnvFile();
        string Get(string key, string fallback) =>
            Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v
            : file.TryGetValue(key, out var f) && f.Length > 0 ? f
            : fallback;

        BaseUrl       = Get("E2E_BASE_URL",     "https://localhost:18443");
        MailpitUrl    = Get("E2E_MAILPIT_URL",  "http://localhost:18025");
        WireMockUrl   = Get("E2E_WIREMOCK_URL", "http://localhost:18080");
        AdminEmail    = Get("ADMIN_SEED_EMAIL",    "");
        AdminPassword = Get("ADMIN_SEED_PASSWORD", "");
        Browser       = Get("E2E_BROWSER",      "chromium");

        if (AdminEmail.Length == 0 || AdminPassword.Length == 0)
            throw new InvalidOperationException("Faltan ADMIN_SEED_EMAIL/ADMIN_SEED_PASSWORD para la corrida E2E.");
    }

    private static Dictionary<string, string> LoadEnvFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "e2e.env")))
            dir = dir.Parent;
        if (dir == null) return [];

        return File.ReadAllLines(Path.Combine(dir.FullName, "e2e.env"))
            .Where(l => l.Contains('=') && !l.TrimStart().StartsWith('#'))
            .Select(l => l.Split('=', 2))
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim());
    }
}
