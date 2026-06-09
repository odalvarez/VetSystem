using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<IEnumerable<User>> ListByRoleAsync(UserRole role, CancellationToken ct = default);
    Task<(IEnumerable<User> Data, int Total)> ListAllAsync(
        string? role, string? search, bool? isActive,
        int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
