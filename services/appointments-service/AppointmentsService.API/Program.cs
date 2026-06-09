using System.Text;
using AppointmentsService.API.Middleware;
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
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();

// Cliente HTTP hacia notifications-service; incluye X-Internal-Key para pasar el middleware
var notifBaseUrl    = builder.Configuration["NotificationsService:BaseUrl"]    ?? "http://notifications-service:5004";
var notifInternalKey = builder.Configuration["NotificationsService:InternalKey"] ?? "dev-internal-key-change-in-production";
builder.Services.AddHttpClient<INotificationClient, NotificationHttpClient>(c =>
{
    c.BaseAddress = new Uri(notifBaseUrl);
    c.DefaultRequestHeaders.Add("X-Internal-Key", notifInternalKey);
    // Evolution API a veces tarda; 10s es suficiente para no bloquear el hilo principal
    c.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<AppointmentAppService>();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
