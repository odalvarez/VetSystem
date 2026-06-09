using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VetSystem.Frontend;
using VetSystem.Frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Singleton: la info del usuario debe vivir durante toda la sesión del navegador
builder.Services.AddSingleton<TokenProvider>();

// Proveedor de estado de autenticación que conecta nuestro token con [Authorize] de Blazor
builder.Services.AddScoped<AuthenticationStateProvider, VetAuthStateProvider>();
builder.Services.AddAuthorizationCore();

// Todos los servicios usan el mismo origen (el propio nginx).
// Las cookies httpOnly se envían automáticamente — no hace falta ningún handler.
var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddHttpClient<AuthApiClient>(c => c.BaseAddress = baseAddress);
builder.Services.AddHttpClient<PatientApiClient>(c => c.BaseAddress = baseAddress);
builder.Services.AddHttpClient<AppointmentApiClient>(c => c.BaseAddress = baseAddress);
builder.Services.AddHttpClient<NotificationApiClient>(c => c.BaseAddress = baseAddress);

await builder.Build().RunAsync();
