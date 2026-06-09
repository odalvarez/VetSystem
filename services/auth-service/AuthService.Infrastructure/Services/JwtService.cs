using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;

    // 8 horas — cubre una jornada laboral completa sin forzar re-login
    public int ExpiresInSeconds { get; } = 28800;

    public JwtService(IConfiguration config)
    {
        _secret   = config["Jwt:Secret"]   ?? throw new InvalidOperationException("Jwt:Secret no configurado.");
        _issuer   = config["Jwt:Issuer"]   ?? "auth-service";
        _audience = config["Jwt:Audience"] ?? "vetsystem";
    }

    public string GenerateToken(User user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            // ClaimTypes.Name es lo que PatientsController y AppointmentsController usan para "ownerName / vetName"
            new Claim(ClaimTypes.Name,               user.FullName),
            new Claim("phone",                       user.Phone ?? ""),
            new Claim(ClaimTypes.Role,               user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddSeconds(ExpiresInSeconds),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
