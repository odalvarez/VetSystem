using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    int ExpiresInSeconds { get; }
}
