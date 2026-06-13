using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using NotificationsService.API.Middleware;
using NotificationsService.Application.Interfaces;
using NotificationsService.Application.Services;
using NotificationsService.Infrastructure.Data;
using NotificationsService.Infrastructure.Data.Repositories;
using NotificationsService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "VetSystem — Notifications Service",
        Version     = "v1",
        Description = "Envío de notificaciones por WhatsApp (Evolution API) y correo (SMTP), y programación de recordatorios automáticos de citas."
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

builder.Services.AddDbContext<NotificationsDbContext>(opt =>
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
            ClockSkew                = TimeSpan.Zero
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

builder.Services.AddHttpClient<IWhatsAppSender, EvolutionWhatsAppSender>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<NotificationAppService>();

// Worker que despierta cada 5 minutos y despacha los recordatorios cuya hora ya llegó
builder.Services.AddHostedService<NotificationsService.API.ReminderWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    if (!app.Environment.IsEnvironment("Testing"))
        await MigrateWithRetryAsync(db.Database);
}

// InternalKeyMiddleware debe estar antes de Authentication para rechazar temprano
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "NotificationsService v1"));
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<InternalKeyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// El healthcheck se expone sin autenticación para que Docker pueda chequearlo
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

// EF Core en SQL Server puede intentar crear la BD justo cuando esta ya fue creada
// por un intento anterior (contenedor reiniciado a mitad de migración).
// El error 1801 significa "la BD ya existe": en el reintento ExistsAsync() ya devuelve true
// y MigrateAsync() omite el CREATE DATABASE y aplica solo las migraciones pendientes.
static async Task MigrateWithRetryAsync(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade database)
{
    for (int attempt = 1; attempt <= 3; attempt++)
    {
        try
        {
            await database.MigrateAsync();
            return;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 1801 && attempt < 3)
        {
            await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
        }
    }
    await database.MigrateAsync();
}
