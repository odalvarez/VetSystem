using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PatientsService.Application.DTOs;
using PatientsService.Tests.Helpers;

namespace PatientsService.Tests;

public class PatientsControllerTests : IClassFixture<PatientsWebFactory>
{
    private readonly PatientsWebFactory _factory;

    public PatientsControllerTests(PatientsWebFactory factory) => _factory = factory;

    private HttpClient ClientAs(Guid userId, string email, string role) =>
        AuthorizedClient(JwtTestHelper.Generate(userId, email, role, $"{role} Test"));

    private HttpClient AuthorizedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ── Auth guard ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListPatients_Unauthenticated_Returns401()
    {
        var resp = await _factory.CreateClient().GetAsync("/api/patients");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Vet: crear mascota ────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePatient_AsVet_Returns201()
    {
        var client = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");

        var resp = await client.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name      = "Luna",
            Species   = "Canine",
            Breed     = "Labrador",
            BirthDate = new DateOnly(2020, 3, 15),
            Sex       = "Female",
            WeightKg  = 25.5m,
            OwnerId   = PatientsWebFactory.OwnerId,
            OwnerName = "Pedro Ramirez",
            OwnerPhone = "3001234567"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task CreatePatient_AsAdmin_Returns201()
    {
        var client = ClientAs(PatientsWebFactory.AdminId, "admin@test.com", "Admin");

        var resp = await client.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name      = "Max",
            Species   = "Canine",
            Breed     = "Beagle",
            BirthDate = new DateOnly(2021, 6, 1),
            Sex       = "Male",
            WeightKg  = 12.0m,
            OwnerId   = PatientsWebFactory.OwnerId,
            OwnerName = "Ana García",
            OwnerPhone = "3009876543"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task CreatePatient_VetWithoutOwnerId_Returns400()
    {
        var client = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");

        // El veterinario debe proveer OwnerId; sin él debe recibir 400
        var resp = await client.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name      = "Michi",
            Species   = "Feline",
            Breed     = "Siames",
            BirthDate = new DateOnly(2022, 1, 10),
            Sex       = "Female",
            WeightKg  = 4.0m
            // Sin OwnerId ni OwnerName
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Owner: crear mascota propia ───────────────────────────────────────────

    [Fact]
    public async Task CreatePatient_AsOwner_Returns201()
    {
        // El owner toma su ID del JWT; no necesita enviar OwnerId
        var client = ClientAs(PatientsWebFactory.OwnerId, "owner@test.com", "Owner");

        var resp = await client.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name      = "Toby",
            Species   = "Canine",
            Breed     = "Poodle",
            BirthDate = new DateOnly(2019, 11, 5),
            Sex       = "Male",
            WeightKg  = 8.0m
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    // ── Listar mascotas ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListPatients_AsVet_Returns200()
    {
        var client = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp   = await client.GetAsync("/api/patients");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ListPatients_AsOwner_Returns200()
    {
        var client = ClientAs(PatientsWebFactory.OwnerId, "owner@test.com", "Owner");
        var resp   = await client.GetAsync("/api/patients");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Especie ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListSpecies_Authenticated_Returns200()
    {
        var client = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp   = await client.GetAsync("/api/species");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ListSpecies_Unauthenticated_Returns401()
    {
        var resp = await _factory.CreateClient().GetAsync("/api/species");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
