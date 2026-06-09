using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VetSystem.Frontend;
using VetSystem.Frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Lee las URLs de los microservicios desde appsettings.json
var config = builder.Configuration;

// Singleton: el token debe vivir durante toda la sesión del navegador
builder.Services.AddSingleton<TokenProvider>();
builder.Services.AddSingleton<AuthTokenHandler>();

// Proveedor de estado de autenticación que conecta nuestro token con [Authorize] de Blazor
builder.Services.AddScoped<AuthenticationStateProvider, VetAuthStateProvider>();
builder.Services.AddAuthorizationCore();

// HttpClient tipado por servicio, cada uno con su URL base y el handler del token
builder.Services.AddHttpClient<AuthApiClient>(c =>
    c.BaseAddress = new Uri(config["Services:AuthService"] ?? "http://localhost:5001/"))
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<PatientApiClient>(c =>
    c.BaseAddress = new Uri(config["Services:PatientsService"] ?? "http://localhost:5002/"))
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<AppointmentApiClient>(c =>
    c.BaseAddress = new Uri(config["Services:AppointmentsService"] ?? "http://localhost:5003/"))
    .AddHttpMessageHandler<AuthTokenHandler>();

// El notifications-service valida X-Internal-Key en cada request.
// NOTA: en Blazor WASM appsettings.json es un archivo público descargado por el
// navegador, así que esta clave no es un secreto real. Para el prototipo está bien;
// en producción el frontend no debería llamar al notifications-service directamente.
var internalKey = config["Services:InternalKey"] ?? "dev-internal-key-change-in-production";
builder.Services.AddHttpClient<NotificationApiClient>(c =>
{
    c.BaseAddress = new Uri(config["Services:NotificationsService"] ?? "http://localhost:5004/");
    c.DefaultRequestHeaders.Add("X-Internal-Key", internalKey);
}).AddHttpMessageHandler<AuthTokenHandler>();

await builder.Build().RunAsync();
