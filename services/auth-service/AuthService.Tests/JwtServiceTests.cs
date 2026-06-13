using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Tests;

public class JwtServiceTests
{
    private readonly JwtService _svc;
    private readonly string     _secret  = "test-super-secret-key-at-least-32-chars!!";
    private readonly string     _issuer  = "VetSystem";
    private readonly string     _audience = "VetSystem";

    public JwtServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]   = _secret,
                ["Jwt:Issuer"]   = _issuer,
                ["Jwt:Audience"] = _audience
            })
            .Build();

        _svc = new JwtService(config);
    }

    [Fact]
    public void GenerateToken_ProducesValidJwt()
    {
        var user  = CreateTestUser("vet@test.com", UserRole.Veterinarian);
        var token = _svc.GenerateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));

        var handler    = new JwtSecurityTokenHandler();
        var parsed     = handler.ReadJwtToken(token);
        Assert.Equal(_issuer,   parsed.Issuer);
        Assert.Equal(user.Id.ToString(), parsed.Subject);
    }

    [Fact]
    public void GenerateToken_ContainsRoleClaim()
    {
        var user  = CreateTestUser("admin@test.com", UserRole.Admin);
        var token = _svc.GenerateToken(user);

        var principal = ValidateToken(token);
        var role      = principal.FindFirst(ClaimTypes.Role)?.Value;

        Assert.Equal("Admin", role);
    }

    [Fact]
    public void GenerateToken_ContainsEmailClaim()
    {
        var email = "owner@test.com";
        var user  = CreateTestUser(email, UserRole.Owner);
        var token = _svc.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var parsed  = handler.ReadJwtToken(token);
        var emailClaim = parsed.Claims.FirstOrDefault(c =>
            c.Type == JwtRegisteredClaimNames.Email || c.Type == "email");

        Assert.NotNull(emailClaim);
        Assert.Equal(email, emailClaim.Value);
    }

    [Fact]
    public void GenerateToken_ExpiresIn8Hours()
    {
        var user  = CreateTestUser("vet@test.com", UserRole.Veterinarian);
        var token = _svc.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var parsed  = handler.ReadJwtToken(token);

        var remaining = parsed.ValidTo - DateTime.UtcNow;
        Assert.True(remaining.TotalHours >= 7.9 && remaining.TotalHours <= 8.1);
    }

    [Fact]
    public void GenerateToken_TamperedToken_FailsValidation()
    {
        var user  = CreateTestUser("vet@test.com", UserRole.Veterinarian);
        var token = _svc.GenerateToken(user) + "tampered";

        Assert.ThrowsAny<Exception>(() => ValidateToken(token));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static User CreateTestUser(string email, UserRole role) =>
        User.Create("Test", "User", email, "hashedpassword", "3001234567", role);

    private ClaimsPrincipal ValidateToken(string token)
    {
        var handler    = new JwtSecurityTokenHandler();
        var key        = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var validation = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = key,
            ValidateIssuer           = true,
            ValidIssuer              = _issuer,
            ValidateAudience         = true,
            ValidAudience            = _audience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
        return handler.ValidateToken(token, validation, out _);
    }
}
