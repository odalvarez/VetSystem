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

    // ── Bitácoras — AppointmentId ─────────────────────────────────────────────

    private async Task<(HttpClient client, PatientResponse patient)> CreateTestPatient()
    {
        var client  = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp    = await client.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name = "LogPatient", Species = "Canine", Breed = "Beagle",
            BirthDate = new DateOnly(2021, 1, 1), Sex = "Male", WeightKg = 10m,
            OwnerId = PatientsWebFactory.OwnerId, OwnerName = "Log Owner", OwnerPhone = "3000000000"
        });
        var patient = await resp.Content.ReadFromJsonAsync<PatientResponse>();
        return (client, patient!);
    }

    [Fact]
    public async Task CreateLog_WithAppointmentId_ReturnsAppointmentIdInResponse()
    {
        var (client, patient) = await CreateTestPatient();
        var apptId = Guid.NewGuid();

        var resp = await client.PostAsJsonAsync($"/api/patients/{patient.Id}/logs", new CreateConsultationLogRequest
        {
            AppointmentId = apptId,
            ReasonForVisit = "Chequeo post-cita"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var log = await resp.Content.ReadFromJsonAsync<ConsultationLogResponse>();
        Assert.Equal(apptId, log!.AppointmentId);
    }

    [Fact]
    public async Task CreateLog_DuplicateAppointmentId_Returns400()
    {
        var (client, patient) = await CreateTestPatient();
        var apptId = Guid.NewGuid();

        await client.PostAsJsonAsync($"/api/patients/{patient.Id}/logs", new CreateConsultationLogRequest
        {
            AppointmentId = apptId, ReasonForVisit = "Primera"
        });

        // Segunda con mismo AppointmentId debe fallar
        var resp2 = await client.PostAsJsonAsync($"/api/patients/{patient.Id}/logs", new CreateConsultationLogRequest
        {
            AppointmentId = apptId, ReasonForVisit = "Duplicado"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp2.StatusCode);
    }

    [Fact]
    public async Task GetByAppointment_ExistingLog_Returns200()
    {
        var (client, patient) = await CreateTestPatient();
        var apptId = Guid.NewGuid();

        await client.PostAsJsonAsync($"/api/patients/{patient.Id}/logs", new CreateConsultationLogRequest
        {
            AppointmentId = apptId, ReasonForVisit = "Cita de prueba"
        });

        var resp = await client.GetAsync($"/api/patients/{patient.Id}/logs/by-appointment/{apptId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var log = await resp.Content.ReadFromJsonAsync<ConsultationLogResponse>();
        Assert.Equal(apptId, log!.AppointmentId);
        Assert.Equal("Cita de prueba", log.ReasonForVisit);
    }

    [Fact]
    public async Task GetByAppointment_NoLog_Returns404()
    {
        var (client, patient) = await CreateTestPatient();
        var fakeApptId = Guid.NewGuid();

        var resp = await client.GetAsync($"/api/patients/{patient.Id}/logs/by-appointment/{fakeApptId}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task CreateLog_WithoutAppointmentId_AppointmentIdIsNull()
    {
        var (client, patient) = await CreateTestPatient();

        var resp = await client.PostAsJsonAsync($"/api/patients/{patient.Id}/logs", new CreateConsultationLogRequest
        {
            ReasonForVisit = "Sin cita asociada"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var log = await resp.Content.ReadFromJsonAsync<ConsultationLogResponse>();
        Assert.Null(log!.AppointmentId);
    }

    // ── P1: Validaciones de creación y permisos ───────────────────────────────

    [Fact]
    public async Task CreatePatient_InvalidSpeciesId_Returns400()
    {
        var client = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");

        var resp = await client.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "EspecieInvalida",
            Species    = "dragón-no-existe-xyzabc",
            Breed      = "Ninguna",
            BirthDate  = new DateOnly(2020, 1, 1),
            Sex        = "Male",
            WeightKg   = 5m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Test Owner",
            OwnerPhone = "3001111111"
        });

        Assert.True(
            resp.StatusCode == HttpStatusCode.BadRequest || resp.StatusCode == HttpStatusCode.NotFound,
            $"Se esperaba 400 o 404 pero llegó {(int)resp.StatusCode}");
    }

    [Fact]
    public async Task GetRecord_AsOwner_ForDifferentOwner_Returns403()
    {
        var vet    = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");

        // Crea mascota que pertenece a Owner B (un ID distinto al OwnerId fijo del factory)
        var ownerBId = Guid.NewGuid();
        var create   = await vet.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "MascotaOwnerB",
            Species    = "Canine",
            Breed      = "Mestizo",
            BirthDate  = new DateOnly(2020, 4, 10),
            Sex        = "Female",
            WeightKg   = 9m,
            OwnerId    = ownerBId,
            OwnerName  = "Owner B",
            OwnerPhone = "3002222222"
        });
        var patient = await create.Content.ReadFromJsonAsync<PatientResponse>();

        var addRec = await vet.PostAsJsonAsync($"/api/patients/{patient!.Id}/records", new CreateClinicalRecordRequest
        {
            Date      = DateTime.UtcNow,
            Reason    = "Revisión",
            Diagnosis = "Sano",
            Treatment = "Ninguno"
        });
        var record = await addRec.Content.ReadFromJsonAsync<ClinicalRecordResponse>();

        // Owner A intenta ver el registro de una mascota de Owner B
        var ownerA = ClientAs(PatientsWebFactory.OwnerId, "ownera@test.com", "Owner");
        var resp   = await ownerA.GetAsync($"/api/patients/{patient.Id}/records/{record!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteByOwner_AsAdmin_Returns204()
    {
        var vet   = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var admin = ClientAs(PatientsWebFactory.AdminId, "admin@test.com", "Admin");

        var targetOwnerId = Guid.NewGuid();

        // Crea dos mascotas para el mismo dueño
        foreach (var name in new[] { "CascadeA", "CascadeB" })
        {
            await vet.PostAsJsonAsync("/api/patients", new CreatePatientRequest
            {
                Name       = name,
                Species    = "Feline",
                Breed      = "Mestizo",
                BirthDate  = new DateOnly(2021, 2, 2),
                Sex        = "Male",
                WeightKg   = 4m,
                OwnerId    = targetOwnerId,
                OwnerName  = "Cascade Owner",
                OwnerPhone = "3003333333"
            });
        }

        var resp = await admin.DeleteAsync($"/api/patients/by-owner/{targetOwnerId}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteByOwner_AsVet_Returns403()
    {
        var vet  = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var resp = await vet.DeleteAsync($"/api/patients/by-owner/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── P2: Ciclo de vida de bitácoras ────────────────────────────────────────

    [Fact]
    public async Task CloseLog_AlreadyClosed_Returns400()
    {
        var (client, patient) = await CreateTestPatient();

        var create = await client.PostAsJsonAsync($"/api/patients/{patient.Id}/logs", new CreateConsultationLogRequest
        {
            ReasonForVisit = "Bitácora para cerrar dos veces"
        });
        var log = await create.Content.ReadFromJsonAsync<ConsultationLogResponse>();

        // Primer cierre — debe funcionar
        await client.PatchAsync($"/api/patients/{patient.Id}/logs/{log!.Id}/close", null);

        // Segundo cierre — debe fallar
        var resp = await client.PatchAsync($"/api/patients/{patient.Id}/logs/{log.Id}/close", null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateLog_WhenClosed_Returns400()
    {
        var (client, patient) = await CreateTestPatient();

        var create = await client.PostAsJsonAsync($"/api/patients/{patient.Id}/logs", new CreateConsultationLogRequest
        {
            ReasonForVisit = "Bitácora que se cerrará"
        });
        var log = await create.Content.ReadFromJsonAsync<ConsultationLogResponse>();

        await client.PatchAsync($"/api/patients/{patient.Id}/logs/{log!.Id}/close", null);

        // Intento de edición después del cierre
        var resp = await client.PutAsJsonAsync($"/api/patients/{patient.Id}/logs/{log.Id}", new UpdateConsultationLogRequest
        {
            ReasonForVisit = "Intento de modificar cerrada"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── P3: Validaciones de peso y pertenencia ────────────────────────────────

    [Fact]
    public async Task CreatePatient_WeightZero_Returns400()
    {
        var client = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");

        var resp = await client.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "PesoZero",
            Species    = "Canine",
            Breed      = "Mestizo",
            BirthDate  = new DateOnly(2021, 5, 5),
            Sex        = "Male",
            WeightKg   = 0m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Test Owner",
            OwnerPhone = "3004444444"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreatePatient_NegativeWeight_Returns400()
    {
        var client = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");

        var resp = await client.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "PesoNegativo",
            Species    = "Canine",
            Breed      = "Mestizo",
            BirthDate  = new DateOnly(2021, 5, 5),
            Sex        = "Male",
            WeightKg   = -1m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Test Owner",
            OwnerPhone = "3005555555"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetByAppointment_LogBelongsToDifferentPatient_Returns404()
    {
        var (client, patientA) = await CreateTestPatient();
        var (_, patientB)      = await CreateTestPatient();
        var apptId             = Guid.NewGuid();

        // La bitácora se crea asociada a patientB
        await client.PostAsJsonAsync($"/api/patients/{patientB.Id}/logs", new CreateConsultationLogRequest
        {
            AppointmentId  = apptId,
            ReasonForVisit = "Cita de paciente B"
        });

        // Se consulta bajo patientA — debe devolver 404 porque no le pertenece
        var resp = await client.GetAsync($"/api/patients/{patientA.Id}/logs/by-appointment/{apptId}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── P4: Restricciones de especie y edad ───────────────────────────────────

    [Fact]
    public async Task DeleteSpecies_WithPatientsAssigned_Returns409OrError()
    {
        var admin  = ClientAs(PatientsWebFactory.AdminId, "admin@test.com", "Admin");
        var vet    = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");

        // Crea una especie nueva para no afectar "Canine"/"Feline" del seed
        var createSpecies = await admin.PostAsJsonAsync("/api/species", new CreateSpeciesRequest { Name = "Iguana" });
        var species       = await createSpecies.Content.ReadFromJsonAsync<SpeciesResponse>();

        // Crea una mascota con esa especie para que quede referenciada
        await vet.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "IguanaTest",
            Species    = species!.Slug,
            Breed      = "Verde",
            BirthDate  = new DateOnly(2020, 3, 3),
            Sex        = "Male",
            WeightKg   = 2m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Test Owner",
            OwnerPhone = "3006666666"
        });

        // Intenta eliminar la especie con mascotas — el backend bloquea con 400
        var resp = await admin.DeleteAsync($"/api/species/{species.Id}");
        Assert.True(
            resp.StatusCode == HttpStatusCode.Conflict || resp.StatusCode == HttpStatusCode.BadRequest,
            $"Se esperaba 409 o 400 pero llegó {(int)resp.StatusCode}");
    }

    [Fact]
    public async Task GetPatient_Newborn_AgeFieldPresent()
    {
        var vet    = ClientAs(PatientsWebFactory.VetId, "vet@test.com", "Veterinarian");
        var today  = DateOnly.FromDateTime(DateTime.UtcNow);

        var create = await vet.PostAsJsonAsync("/api/patients", new CreatePatientRequest
        {
            Name       = "Recién Nacido",
            Species    = "Canine",
            Breed      = "Mestizo",
            BirthDate  = today,
            Sex        = "Male",
            WeightKg   = 0.5m,
            OwnerId    = PatientsWebFactory.OwnerId,
            OwnerName  = "Test Owner",
            OwnerPhone = "3007777777"
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var patient = await create.Content.ReadFromJsonAsync<PatientResponse>();
        Assert.NotNull(patient);
        // AgeYears debe ser 0 para un recién nacido — no debe arrojar excepción
        Assert.Equal(0, patient!.AgeYears);
    }
}
