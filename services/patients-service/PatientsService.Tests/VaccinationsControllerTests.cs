using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PatientsService.Application.DTOs;
using PatientsService.Tests.Helpers;

namespace PatientsService.Tests;

public class VaccinationsControllerTests : IClassFixture<PatientsWebFactory>
{
    private readonly PatientsWebFactory _factory;
    private readonly HttpClient _vet;
    private readonly HttpClient _admin;
    private readonly HttpClient _owner;
    private readonly HttpClient _anon;

    public VaccinationsControllerTests(PatientsWebFactory factory)
    {
        _factory = factory;
        _vet     = ClientAs(PatientsWebFactory.VetId,   "vet@test.com",   "Veterinarian");
        _admin   = ClientAs(PatientsWebFactory.AdminId, "admin@test.com", "Admin");
        _owner   = ClientAs(PatientsWebFactory.OwnerId, "owner@test.com", "Owner");
        _anon    = factory.CreateClient();
    }

    private HttpClient ClientAs(Guid userId, string email, string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.Generate(userId, email, role, $"{role} Test"));
        return client;
    }

    // ── Catálogo ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListDefinitions_Unauthenticated_Returns401()
    {
        var resp = await _anon.GetAsync("/api/vaccines");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ListDefinitions_AsVet_Returns200()
    {
        var resp = await _vet.GetAsync("/api/vaccines");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task CreateDefinition_AsAdmin_Returns201()
    {
        var req = new CreateVaccineDefinitionRequest(
            Name: "Rabia", Description: "Vacuna antirrábica", Scheme: "Annual",
            AnnualIntervalMonths: 12, DoseSteps: null);

        var resp = await _admin.PostAsJsonAsync("/api/vaccines", req);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<VaccineDefinitionResponse>();
        Assert.NotNull(body);
        Assert.Equal("Rabia", body!.Name);
        Assert.Equal("Annual", body.Scheme);
    }

    [Fact]
    public async Task CreateDefinition_AsVet_Returns403()
    {
        var req = new CreateVaccineDefinitionRequest(
            "Test", null, "Annual", 12, null);
        var resp = await _vet.PostAsJsonAsync("/api/vaccines", req);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task CreateDefinition_MultiDose_StoresSteps()
    {
        var steps = new List<VaccineDoseStepDto>
        {
            new(1, 0),   // dosis base (no se guarda como paso)
            new(2, 28),
            new(3, 28)
        };
        var req = new CreateVaccineDefinitionRequest(
            "Distemper/Parvo", "Esquema triple", "MultiDose", 12, steps);

        var resp = await _admin.PostAsJsonAsync("/api/vaccines", req);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<VaccineDefinitionResponse>();
        Assert.NotNull(body);
        Assert.Equal("MultiDose", body!.Scheme);
        // La dosis 1 no se guarda como paso; se guardan dosis 2 y 3
        Assert.Equal(2, body.DoseSteps.Count);
    }

    // ── Registro de vacunas ───────────────────────────────────────────────────

    [Fact]
    public async Task RegisterVaccination_AsVet_Returns201WithNextDueDate()
    {
        // Crea paciente
        var patientId = await CreatePatientAsync();

        // Crea vacuna anual
        var defReq  = new CreateVaccineDefinitionRequest("Tos de las Perreras", null, "Annual", 12, null);
        var defResp = await _admin.PostAsJsonAsync("/api/vaccines", defReq);
        var def     = await defResp.Content.ReadFromJsonAsync<VaccineDefinitionResponse>();

        // Registra dosis
        var req = new RegisterVaccinationRequest(
            VaccineDefinitionId: def!.Id,
            AdministeredAt:      "2026-01-15",
            BatchNumber:         "LOT-001",
            Notes:               null,
            NextDueDateOverride: null);

        var resp = await _vet.PostAsJsonAsync($"/api/patients/{patientId}/vaccinations", req);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<VaccinationRecordResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.DoseNumber);
        Assert.Equal("2027-01-15", body.NextDueDate); // +12 meses
    }

    [Fact]
    public async Task RegisterVaccination_VetCanOverrideNextDueDate()
    {
        var patientId = await CreatePatientAsync();

        var defReq  = new CreateVaccineDefinitionRequest("Leptospira", null, "Annual", 12, null);
        var defResp = await _admin.PostAsJsonAsync("/api/vaccines", defReq);
        var def     = await defResp.Content.ReadFromJsonAsync<VaccineDefinitionResponse>();

        var req = new RegisterVaccinationRequest(
            VaccineDefinitionId: def!.Id,
            AdministeredAt:      "2026-01-15",
            BatchNumber:         null,
            Notes:               null,
            NextDueDateOverride: "2026-08-01"); // vet ajusta la fecha

        var resp = await _vet.PostAsJsonAsync($"/api/patients/{patientId}/vaccinations", req);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<VaccinationRecordResponse>();
        Assert.Equal("2026-08-01", body!.NextDueDate);
    }

    [Fact]
    public async Task RegisterVaccination_SingleDose_NoNextDueDate()
    {
        var patientId = await CreatePatientAsync();

        var defReq  = new CreateVaccineDefinitionRequest("Leishmaniasis", null, "SingleDose", 12, null);
        var defResp = await _admin.PostAsJsonAsync("/api/vaccines", defReq);
        var def     = await defResp.Content.ReadFromJsonAsync<VaccineDefinitionResponse>();

        var req = new RegisterVaccinationRequest(def!.Id, "2026-03-01", null, null, null);

        var resp = await _vet.PostAsJsonAsync($"/api/patients/{patientId}/vaccinations", req);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<VaccinationRecordResponse>();
        Assert.Null(body!.NextDueDate);
    }

    [Fact]
    public async Task RegisterVaccination_AsOwner_Returns403()
    {
        var patientId = await CreatePatientAsync();

        var defReq  = new CreateVaccineDefinitionRequest("Test403", null, "Annual", 12, null);
        var defResp = await _admin.PostAsJsonAsync("/api/vaccines", defReq);
        var def     = await defResp.Content.ReadFromJsonAsync<VaccineDefinitionResponse>();

        var req = new RegisterVaccinationRequest(def!.Id, "2026-01-01", null, null, null);
        var resp = await _owner.PostAsJsonAsync($"/api/patients/{patientId}/vaccinations", req);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task ListVaccinations_AsVet_Returns200()
    {
        var patientId = await CreatePatientAsync();
        var resp = await _vet.GetAsync($"/api/patients/{patientId}/vaccinations");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteVaccination_AsAdmin_Returns204()
    {
        var patientId = await CreatePatientAsync();

        var defReq  = new CreateVaccineDefinitionRequest("DeleteMe", null, "Annual", 12, null);
        var defResp = await _admin.PostAsJsonAsync("/api/vaccines", defReq);
        var def     = await defResp.Content.ReadFromJsonAsync<VaccineDefinitionResponse>();

        var regReq  = new RegisterVaccinationRequest(def!.Id, "2026-01-01", null, null, null);
        var regResp = await _vet.PostAsJsonAsync($"/api/patients/{patientId}/vaccinations", regReq);
        var record  = await regResp.Content.ReadFromJsonAsync<VaccinationRecordResponse>();

        var del = await _admin.DeleteAsync($"/api/patients/{patientId}/vaccinations/{record!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task<Guid> CreatePatientAsync()
    {
        var req = new
        {
            name        = "Buddy",
            species     = "canine",
            breed       = "Labrador",
            birthDate   = "2022-05-10",
            sex         = "Male",
            weightKg    = 25.0m,
            ownerId     = PatientsWebFactory.OwnerId,
            ownerName   = "Carlos López",
            ownerPhone  = "3001234567"
        };
        var resp   = await _vet.PostAsJsonAsync("/api/patients", req);
        var body   = await resp.Content.ReadFromJsonAsync<PatientResponse>();
        return body!.Id;
    }
}
