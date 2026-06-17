using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using PatientsService.API;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PatientsService.API.Middleware;
using PatientsService.Application.Interfaces;
using PatientsService.Application.Services;
using PatientsService.Infrastructure.Data;
using PatientsService.Infrastructure.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "VetSystem — Patients Service",
        Version     = "v1",
        Description = "Registro de mascotas, historias clínicas y bitácoras de consulta. Un Owner solo accede a sus propias mascotas; Veterinarian y Admin acceden a todas."
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
     .WithHeaders("Content-Type", "Authorization")
     .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
     .AllowCredentials()));

builder.Services.AddDbContext<PatientsDbContext>(opt =>
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

builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<ISpeciesRepository, SpeciesRepository>();
builder.Services.AddScoped<IConsultationLogRepository, ConsultationLogRepository>();
builder.Services.AddScoped<PatientAppService>();
builder.Services.AddScoped<SpeciesAppService>();
builder.Services.AddScoped<ConsultationLogAppService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();
    if (!app.Environment.IsEnvironment("Testing"))
    {
        await MigrateWithRetryAsync(db.Database);
        await SpeciesSeeder.SeedAsync(db);
    }
}

app.UseCors();
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "PatientsService v1"));
}
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
