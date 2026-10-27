using System.Net;
using VetSystem.E2E.Infrastructure;

namespace VetSystem.E2E.Regression;

// Toda ruta de API que use el frontend debe llegar a un microservicio. Si nginx no la conoce,
// la petición cae en el SPA y devuelve index.html con 200, un fallo silencioso (así se rompió /api/vaccines).
[Collection(StackCollection.Name)]
public class ApiRoutingTests(StackFixture stack) : E2ETest(stack)
{
    [Theory]
    [InlineData("/api/auth/me")]
    [InlineData("/api/patients")]
    [InlineData("/api/species")]
    [InlineData("/api/vaccines")]
    [InlineData("/api/appointments")]
    [InlineData("/api/schedules/settings")]
    [InlineData("/api/notifications")]
    public async Task ApiPrefix_IsProxiedToAService_NotToTheSpa(string path)
    {
        using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
        using var http    = new HttpClient(handler) { BaseAddress = new Uri(Stack.Env.BaseUrl) };

        var resp = await http.GetAsync(path);

        Assert.NotEqual("text/html", resp.Content.Headers.ContentType?.MediaType);
        Assert.Contains(resp.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
    }
}
