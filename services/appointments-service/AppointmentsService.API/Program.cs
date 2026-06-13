using System.Text;
using AppointmentsService.API.Middleware;
using Microsoft.OpenApi.Models;
using AppointmentsService.Application.Interfaces;
using AppointmentsService.Application.Services;
using AppointmentsService.Infrastructure.Data;
using AppointmentsService.Infrastructure.Data.Repositories;
using AppointmentsService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "VetSystem — Appointments Service",
        Version     = "v1",
        Description = "Gestión de citas, agenda del veterinario y consulta de disponibilidad de horarios. Un Owner solo ve y gestiona sus propias citas."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "JWT obtenido desde POST /api/auth/login. Formato: Bearer {token}"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
    var xmlPath = Path.Combine(AppContext.BaseDirectory,
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost", "https://localhost")
     .AllowAnyHeader()
     .AllowAnyMethod()));

builder.Services.AddDbContext<AppointmentsDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret no configurado.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero,
            RoleClaimType            = System.Security.Claims.ClaimTypes.Role
        };
        opt.UseSecurityTokenValidators = true;
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // Swagger UI envía el token en el header; el navegador lo envía en la cookie httpOnly
                if (ctx.Request.Headers.TryGetValue("Authorization", out var auth) &&
                    auth.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Token = auth.ToString()["Bearer ".Length..].Trim();
                    return Task.CompletedTask;
                }
                ctx.Token = ctx.Request.Cookies["vetsys_jwt"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();

// Cliente HTTP hacia notifications-service; incluye X-Internal-Key para pasar el middleware
var notifBaseUrl     = builder.Configuration["NotificationsService:BaseUrl"]    ?? "http://notifications-service:8080";
var notifInternalKey = builder.Configuration["NotificationsService:InternalKey"] ?? "";
builder.Services.AddHttpClient<INotificationClient, NotificationHttpClient>(c =>
{
    c.BaseAddress = new Uri(notifBaseUrl);
    c.DefaultRequestHeaders.Add("X-Internal-Key", notifInternalKey);
    c.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<AppointmentAppService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppointmentsDbContext>();
    if (!app.Environment.IsEnvironment("Testing"))
        await MigrateWithRetryAsync(db.Database);
}

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "AppointmentsService v1"));
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

static async Task MigrateWithRetryAsync(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade database)
{
    for (int attempt = 1; attempt <= 3; attempt++)
    {
        try { await database.MigrateAsync(); return; }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 1801 && attempt < 3)
        { await Task.Delay(TimeSpan.FromSeconds(attempt * 2)); }
    }
    await database.MigrateAsync();
}
