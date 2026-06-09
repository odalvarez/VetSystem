using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost", "https://localhost")
     .AllowAnyHeader()
     .AllowAnyMethod()));

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
    await MigrateWithRetryAsync(db.Database);
}

app.UseCors();
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
