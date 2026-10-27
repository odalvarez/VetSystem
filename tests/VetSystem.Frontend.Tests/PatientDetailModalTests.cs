using System.Net;
using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using VetSystem.Frontend.Components;
using VetSystem.Frontend.Models;
using VetSystem.Frontend.Services;

namespace VetSystem.Frontend.Tests;

// La misma ficha se comporta distinto por rol: el propietario solo lee (ficha B) y vet/admin editan
public class PatientDetailModalTests : TestContext
{
    private static readonly PatientResponse Pet = new()
    {
        Id = Guid.NewGuid(), Name = "Luna", Species = "dog", SpeciesName = "Perro", SpeciesIcon = "🐶", Breed = "Mestizo",
        Sex = "female", BirthDate = new DateOnly(2023, 1, 1), WeightKg = 12.5m, Color = "Café", OwnerName = "Camila Rojas"
    };

    private sealed class EmptyApiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = path.EndsWith("vaccinations") || path.EndsWith("vaccines")
                ? "[]"
                : """{"items":[],"totalCount":0,"page":1,"pageSize":50}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private static HttpClient Http() => new(new EmptyApiHandler()) { BaseAddress = new Uri("http://localhost/") };

    private void Arrange(string role)
    {
        Services.AddSingleton(new PatientApiClient(Http()));
        Services.AddSingleton(new ConsultationLogApiClient(Http()));
        Services.AddSingleton(new VaccinationApiClient(Http()));
        Services.AddSingleton(new ModalState());

        var token = new TokenProvider();
        token.SetSession(new LoginUserInfo { Id = Guid.NewGuid(), Role = role, FullName = "Usuario", Email = "u@example.com" });
        Services.AddSingleton(token);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedFragment Render()
    {
        var cut = RenderComponent<ModalHost>(host => host.AddChildContent<PatientDetailModal>(p => p
            .Add(x => x.Patient, Pet)
            .Add(x => x.OnClose, EventCallback.Empty)));
        cut.WaitForAssertion(() => Assert.Contains("admin-tab", cut.Markup));
        return cut;
    }

    [Fact]
    public void Owner_SeesReadOnlySheet_WithoutInputsOrSave()
    {
        Arrange("Owner");

        var cut = Render();

        Assert.NotEmpty(cut.FindAll(".pet-hero"));
        Assert.Empty(cut.FindAll("input.form-control"));
        Assert.DoesNotContain("Guardar cambios", cut.Markup);
        Assert.Contains("Hembra", cut.Markup);
    }

    [Fact]
    public void Veterinarian_EditsWithInputs_ButCannotDelete()
    {
        Arrange("Veterinarian");

        var cut = Render();

        Assert.NotEmpty(cut.FindAll("#pd-weight"));
        Assert.Contains("Guardar cambios", cut.Markup);
        Assert.DoesNotContain("Eliminar mascota", cut.Markup);
    }

    [Fact]
    public void Admin_CanAlsoDelete()
    {
        Arrange("Admin");

        var cut = Render();

        Assert.Contains("Eliminar mascota", cut.Markup);
    }

    [Fact]
    public void Owner_CannotRegisterVaccineDoses()
    {
        Arrange("Owner");
        var cut = Render();

        cut.FindAll("button.admin-tab").First(b => b.TextContent.Contains("Vacunas")).Click();

        Assert.DoesNotContain("Registrar dosis", cut.Markup);
    }
}
