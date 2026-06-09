using System.Text;
using System.Threading.RateLimiting;
using AuthService.API.Middleware;
using AuthService.Application.Interfaces;
using AuthService.Application.Services;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Data.Repositories;
using AuthService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// El frontend Blazor corre en el navegador; sin CORS el navegador bloquea todas las llamadas
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost", "https://localhost")
     .AllowAnyHeader()
     .AllowAnyMethod()));

builder.Services.AddDbContext<AuthDbContext>(opt =>
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
        // Usa el validador clásico JwtSecurityTokenHandler; el nuevo JsonWebTokenHandler
        // en .NET 9 tiene un bug con tokens generados por JwtSecurityTokenHandler
        opt.UseSecurityTokenValidators = true;
        // El JWT llega en la cookie httpOnly; el header Authorization no se usa desde el navegador
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

// Máximo 10 intentos de login por IP en 10 minutos para bloquear ataques de fuerza bruta
builder.Services.AddRateLimiter(opt =>
{
    opt.AddFixedWindowLimiter("login", cfg =>
    {
        cfg.Window            = TimeSpan.FromMinutes(10);
        cfg.PermitLimit       = 10;
        cfg.QueueLimit        = 0;
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    opt.RejectionStatusCode = 429;
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<AuthApplicationService>();

var app = builder.Build();

// Aplicar migraciones pendientes al arrancar; EF las omite si ya están aplicadas
using (var scope = app.Services.CreateScope())
{
    var db      = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var hasher  = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await MigrateWithRetryAsync(db.Database);

    // Crea el admin inicial si no existe ningún usuario con rol Admin
    var adminEmail = app.Configuration["AdminSeed:Email"]    ?? "vet@vetsystem.com";
    var adminPwd   = app.Configuration["AdminSeed:Password"] ?? "Admin1234!";
    if (!await db.Users.AnyAsync(u => u.Role == AuthService.Domain.Enums.UserRole.Admin))
    {
        var adminUser = AuthService.Domain.Entities.User.Create(
            "Administrador", "VetSystem",
            adminEmail, hasher.Hash(adminPwd),
            "", AuthService.Domain.Enums.UserRole.Admin);
        await db.Users.AddAsync(adminUser);
        await db.SaveChangesAsync();
    }
}

app.UseCors();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Docker usa este endpoint para saber si el servicio está listo
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
