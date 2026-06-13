using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _db;

    public UserRepository(AuthDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant() && !u.IsDeleted, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
        _db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant() && !u.IsDeleted, ct);

    public async Task<IEnumerable<User>> ListByRoleAsync(UserRole role, CancellationToken ct) =>
        await _db.Users.Where(u => u.Role == role && !u.IsDeleted).OrderBy(u => u.LastName).ToListAsync(ct);

    public async Task<(IEnumerable<User> Data, int Total)> ListAllAsync(
        string? role, string? search, bool? isActive,
        int page, int pageSize, CancellationToken ct)
    {
        var q = _db.Users.Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var r))
            q = q.Where(u => u.Role == r);

        if (isActive.HasValue)
            q = q.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(u => u.FirstName.Contains(search) ||
                              u.LastName.Contains(search)  ||
                              u.Email.Contains(search));

        var total = await q.CountAsync(ct);
        var data  = await q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return (data, total);
    }

    public async Task AddAsync(User user, CancellationToken ct) =>
        await _db.Users.AddAsync(user, ct);

    public Task UpdateAsync(User user, CancellationToken ct)
    {
        _db.Users.Update(user);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(User user, CancellationToken ct)
    {
        user.SoftDelete();
        _db.Users.Update(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) =>
        _db.SaveChangesAsync(ct);
}
