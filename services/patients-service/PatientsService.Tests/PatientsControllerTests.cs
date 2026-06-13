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

    // ── Crear mascota ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePatient_AsVet_Returns201()
    {
        var client = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");

        var resp = await client.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "Luna",
            Species    = "Canine",
            Breed      = "Labrador",
            BirthDate  = new DateOnly(2020, 3, 15),
            Sex        = "Female",
            WeightKg   = 25.5m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Pedro Ramirez",
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
            Name       = "Max",
            Species    = "Canine",
            Breed      = "Beagle",
            BirthDate  = new DateOnly(2021, 6, 1),
            Sex        = "Male",
            WeightKg   = 12.0m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Ana García",
            OwnerPhone = "3009876543"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task CreatePatient_AsOwner_Returns201()
    {
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

    [Fact]
    public async Task CreatePatient_VetWithoutOwnerId_Returns400()
    {
        var client = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");

        var resp = await client.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name      = "Michi",
            Species   = "Feline",
            Breed     = "Siames",
            BirthDate = new DateOnly(2022, 1, 10),
            Sex       = "Female",
            WeightKg  = 4.0m
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
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

    // ── Obtener mascota por ID ────────────────────────────────────────────────

    [Fact]
    public async Task GetPatient_AsVet_Returns200()
    {
        var vet    = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var create = await vet.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "Rocky",
            Species    = "Canine",
            Breed      = "Bulldog",
            BirthDate  = new DateOnly(2020, 5, 10),
            Sex        = "Male",
            WeightKg   = 20m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Luis",
            OwnerPhone = "3001111111"
        });
        var patient = await create.Content.ReadFromJsonAsync<PatientResponse>();

        var resp = await vet.GetAsync($"/api/patients/{patient!.Id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetPatient_Unauthenticated_Returns401()
    {
        var resp = await _factory.CreateClient().GetAsync($"/api/patients/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Actualizar mascota ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePatient_AsVet_Returns200()
    {
        var vet    = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var create = await vet.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "Bobby",
            Species    = "Canine",
            Breed      = "Pug",
            BirthDate  = new DateOnly(2021, 1, 1),
            Sex        = "Male",
            WeightKg   = 7m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Carlos",
            OwnerPhone = "3002222222"
        });
        var patient = await create.Content.ReadFromJsonAsync<PatientResponse>();

        var resp = await vet.PutAsJsonAsync($"/api/patients/{patient!.Id}", new UpdatePatientRequest
        {
            Name      = "Bobby Updated",
            Breed     = "Pug",
            BirthDate = new DateOnly(2021, 1, 1),
            Sex       = "Male",
            WeightKg  = 7.5m
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Eliminar mascota ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePatient_AsAdmin_Returns204()
    {
        var vet    = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var admin  = ClientAs(PatientsWebFactory.AdminId, "admin@test.com", "Admin");
        var create = await vet.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "ToDelete",
            Species    = "Feline",
            Breed      = "Persa",
            BirthDate  = new DateOnly(2020, 6, 1),
            Sex        = "Female",
            WeightKg   = 3.5m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Maria",
            OwnerPhone = "3003333333"
        });
        var patient = await create.Content.ReadFromJsonAsync<PatientResponse>();

        var resp = await admin.DeleteAsync($"/api/patients/{patient!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task DeletePatient_AsVet_Returns403()
    {
        var vet    = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var create = await vet.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "VetCantDelete",
            Species    = "Canine",
            Breed      = "Beagle",
            BirthDate  = new DateOnly(2021, 1, 1),
            Sex        = "Male",
            WeightKg   = 10m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Luis",
            OwnerPhone = "3005555555"
        });
        var patient = await create.Content.ReadFromJsonAsync<PatientResponse>();

        var resp = await vet.DeleteAsync($"/api/patients/{patient!.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task DeletePatient_AsOwner_Returns403()
    {
        var vet    = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var create = await vet.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "OwnerCantDelete",
            Species    = "Canine",
            Breed      = "Dalmata",
            BirthDate  = new DateOnly(2021, 3, 10),
            Sex        = "Male",
            WeightKg   = 22m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Sofia",
            OwnerPhone = "3004444444"
        });
        var patient = await create.Content.ReadFromJsonAsync<PatientResponse>();

        var owner = ClientAs(PatientsWebFactory.OwnerId, "owner@test.com", "Owner");
        var resp  = await owner.DeleteAsync($"/api/patients/{patient!.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── Historia clínica ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddRecord_AsVet_Returns201()
    {
        var vet    = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var create = await vet.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "Clin",
            Species    = "Canine",
            Breed      = "Mestizo",
            BirthDate  = new DateOnly(2019, 4, 1),
            Sex        = "Male",
            WeightKg   = 15m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Pedro",
            OwnerPhone = "3005555555"
        });
        var patient = await create.Content.ReadFromJsonAsync<PatientResponse>();

        var resp = await vet.PostAsJsonAsync($"/api/patients/{patient!.Id}/records", new CreateClinicalRecordRequest
        {
            Date      = DateTime.UtcNow,
            Reason    = "Control rutinario",
            Diagnosis = "Sano",
            Treatment = "Ninguno"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task AddRecord_AsOwner_Returns403()
    {
        var vet    = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var create = await vet.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "NoClin",
            Species    = "Feline",
            Breed      = "Mestizo",
            BirthDate  = new DateOnly(2020, 7, 15),
            Sex        = "Female",
            WeightKg   = 4m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Ana",
            OwnerPhone = "3006666666"
        });
        var patient = await create.Content.ReadFromJsonAsync<PatientResponse>();

        var owner = ClientAs(PatientsWebFactory.OwnerId, "owner@test.com", "Owner");
        var resp  = await owner.PostAsJsonAsync($"/api/patients/{patient!.Id}/records", new CreateClinicalRecordRequest
        {
            Date      = DateTime.UtcNow,
            Reason    = "Intento de owner",
            Diagnosis = "No permitido",
            Treatment = "N/A"
        });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task ListRecords_AsVet_Returns200()
    {
        var vet    = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var create = await vet.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "RecordsList",
            Species    = "Canine",
            Breed      = "Labrador",
            BirthDate  = new DateOnly(2018, 2, 20),
            Sex        = "Female",
            WeightKg   = 28m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Juan",
            OwnerPhone = "3007777777"
        });
        var patient = await create.Content.ReadFromJsonAsync<PatientResponse>();

        var resp = await vet.GetAsync($"/api/patients/{patient!.Id}/records");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ListRecords_AsOwner_Returns200()
    {
        var vet    = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var create = await vet.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "OwnerRecords",
            Species    = "Feline",
            Breed      = "Angora",
            BirthDate  = new DateOnly(2021, 9, 5),
            Sex        = "Male",
            WeightKg   = 5m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Owner Test",
            OwnerPhone = "3008888888"
        });
        var patient = await create.Content.ReadFromJsonAsync<PatientResponse>();

        var owner = ClientAs(PatientsWebFactory.OwnerId, "owner@test.com", "Owner");
        var resp  = await owner.GetAsync($"/api/patients/{patient!.Id}/records");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetRecord_AsVet_Returns200()
    {
        var vet    = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var create = await vet.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "GetRecordPet",
            Species    = "Canine",
            Breed      = "Golden",
            BirthDate  = new DateOnly(2017, 11, 11),
            Sex        = "Male",
            WeightKg   = 30m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Diego",
            OwnerPhone = "3009999999"
        });
        var patient = await create.Content.ReadFromJsonAsync<PatientResponse>();

        var addRecord = await vet.PostAsJsonAsync($"/api/patients/{patient!.Id}/records", new CreateClinicalRecordRequest
        {
            Date      = DateTime.UtcNow,
            Reason    = "Vacuna",
            Diagnosis = "Sano",
            Treatment = "Vacuna antirrábica"
        });
        var record = await addRecord.Content.ReadFromJsonAsync<ClinicalRecordResponse>();

        var resp = await vet.GetAsync($"/api/patients/{patient.Id}/records/{record!.Id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Especies — GET ────────────────────────────────────────────────────────

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

    // ── Especies — escritura (solo Admin) ────────────────────────────────────

    [Fact]
    public async Task CreateSpecies_AsAdmin_Returns201()
    {
        var client = ClientAs(PatientsWebFactory.AdminId, "admin@test.com", "Admin");
        var resp   = await client.PostAsJsonAsync("/api/species", new CreateSpeciesRequest { Name = "Reptile" });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task CreateSpecies_AsVet_Returns403()
    {
        var client = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp   = await client.PostAsJsonAsync("/api/species", new CreateSpeciesRequest { Name = "ShouldFail" });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task CreateSpecies_AsOwner_Returns403()
    {
        var client = ClientAs(PatientsWebFactory.OwnerId, "owner@test.com", "Owner");
        var resp   = await client.PostAsJsonAsync("/api/species", new CreateSpeciesRequest { Name = "ShouldFail" });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateSpecies_AsAdmin_Returns200()
    {
        var admin  = ClientAs(PatientsWebFactory.AdminId, "admin@test.com", "Admin");
        var create = await admin.PostAsJsonAsync("/api/species", new CreateSpeciesRequest { Name = "UpdateMe" });
        var species = await create.Content.ReadFromJsonAsync<SpeciesResponse>();

        var resp = await admin.PutAsJsonAsync($"/api/species/{species!.Id}", new UpdateSpeciesRequest { Name = "Updated" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateSpecies_AsVet_Returns403()
    {
        var admin  = ClientAs(PatientsWebFactory.AdminId, "admin@test.com", "Admin");
        var create = await admin.PostAsJsonAsync("/api/species", new CreateSpeciesRequest { Name = "VetCantUpdate" });
        var species = await create.Content.ReadFromJsonAsync<SpeciesResponse>();

        var vet  = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp = await vet.PutAsJsonAsync($"/api/species/{species!.Id}", new UpdateSpeciesRequest { Name = "Fail" });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteSpecies_AsAdmin_Returns204()
    {
        var admin  = ClientAs(PatientsWebFactory.AdminId, "admin@test.com", "Admin");
        var create = await admin.PostAsJsonAsync("/api/species", new CreateSpeciesRequest { Name = "DeleteMe" });
        var species = await create.Content.ReadFromJsonAsync<SpeciesResponse>();

        var resp = await admin.DeleteAsync($"/api/species/{species!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteSpecies_AsOwner_Returns403()
    {
        var admin  = ClientAs(PatientsWebFactory.AdminId, "admin@test.com", "Admin");
        var create = await admin.PostAsJsonAsync("/api/species", new CreateSpeciesRequest { Name = "OwnerCantDelete" });
        var species = await create.Content.ReadFromJsonAsync<SpeciesResponse>();

        var owner = ClientAs(PatientsWebFactory.OwnerId, "owner@test.com", "Owner");
        var resp  = await owner.DeleteAsync($"/api/species/{species!.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── Especies — campo Icon ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateSpecies_WithCustomIcon_IconInResponse()
    {
        var admin  = ClientAs(PatientsWebFactory.AdminId, "admin@test.com", "Admin");
        var resp   = await admin.PostAsJsonAsync("/api/species", new CreateSpeciesRequest { Name = "Tortuga", Icon = "🐢" });
        var species = await resp.Content.ReadFromJsonAsync<SpeciesResponse>();

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        Assert.Equal("🐢", species!.Icon);
    }

    [Fact]
    public async Task CreateSpecies_WithoutIcon_DefaultsToFootprint()
    {
        var admin   = ClientAs(PatientsWebFactory.AdminId, "admin@test.com", "Admin");
        var resp    = await admin.PostAsJsonAsync("/api/species", new CreateSpeciesRequest { Name = "Hamster" });
        var species = await resp.Content.ReadFromJsonAsync<SpeciesResponse>();

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        Assert.Equal("🐾", species!.Icon);
    }

    [Fact]
    public async Task UpdateSpecies_WithNewIcon_IconUpdated()
    {
        var admin  = ClientAs(PatientsWebFactory.AdminId, "admin@test.com", "Admin");
        var create = await admin.PostAsJsonAsync("/api/species", new CreateSpeciesRequest { Name = "IconUpdate", Icon = "🐾" });
        var species = await create.Content.ReadFromJsonAsync<SpeciesResponse>();

        var resp    = await admin.PutAsJsonAsync($"/api/species/{species!.Id}", new UpdateSpeciesRequest { Name = "IconUpdate", Icon = "🦊" });
        var updated = await resp.Content.ReadFromJsonAsync<SpeciesResponse>();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("🦊", updated!.Icon);
    }

    [Fact]
    public async Task UpdateSpecies_WithNullIcon_IconUnchanged()
    {
        var admin  = ClientAs(PatientsWebFactory.AdminId, "admin@test.com", "Admin");
        var create = await admin.PostAsJsonAsync("/api/species", new CreateSpeciesRequest { Name = "IconStay", Icon = "🐍" });
        var species = await create.Content.ReadFromJsonAsync<SpeciesResponse>();

        var resp    = await admin.PutAsJsonAsync($"/api/species/{species!.Id}", new UpdateSpeciesRequest { Name = "IconStay", Icon = null });
        var updated = await resp.Content.ReadFromJsonAsync<SpeciesResponse>();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("🐍", updated!.Icon);
    }
}
