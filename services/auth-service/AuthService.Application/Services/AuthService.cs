using AuthService.Application.DTOs;
using AuthService.Application.Exceptions;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;

namespace AuthService.Application.Services;

public class AuthApplicationService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;

    public AuthApplicationService(
        IUserRepository users,
        IPasswordHasher hasher,
        IJwtService jwt)
    {
        _users  = users;
        _hasher = hasher;
        _jwt    = jwt;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        if (await _users.EmailExistsAsync(req.Email, ct))
            throw new ConflictException("El correo ya está registrado.");

        ValidatePasswordStrength(req.Password);

        if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var role))
            throw new ValidationException("Rol inválido.");

        var user = User.Create(
            req.FirstName, req.LastName,
            req.Email, _hasher.Hash(req.Password),
            req.Phone, role);

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        return new RegisterResponse
        {
            Id        = user.Id,
            Email     = user.Email,
            Role      = user.Role.ToString().ToLowerInvariant(),
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(req.Email, ct)
            ?? throw new UnauthorizedException("Credenciales incorrectas.");

        if (!_hasher.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedException("Credenciales incorrectas.");

        // La cuenta puede ser desactivada por un administrador
        if (!user.IsActive)
            throw new UnauthorizedException("Esta cuenta ha sido desactivada. Contacta al administrador.");

        return new LoginResponse
        {
            AccessToken = _jwt.GenerateToken(user),
            ExpiresIn   = _jwt.ExpiresInSeconds,
            User = new LoginUserInfo
            {
                Id       = user.Id,
                Email    = user.Email,
                FullName = user.FullName,
                Role     = user.Role.ToString().ToLowerInvariant(),
                Phone    = user.Phone ?? ""
            }
        };
    }

    public async Task<UserResponse> GetProfileAsync(Guid userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("Usuario no encontrado.");

        return MapToUserResponse(user);
    }

    public async Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest req, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("Usuario no encontrado.");

        user.UpdateProfile(req.FirstName, req.LastName, req.Phone);
        await _users.UpdateAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        return MapToUserResponse(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest req, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("Usuario no encontrado.");

        if (!_hasher.Verify(req.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("La contraseña actual es incorrecta.");

        user.ChangePassword(_hasher.Hash(req.NewPassword));
        await _users.UpdateAsync(user, ct);
        await _users.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<OwnerSummary>> ListOwnersAsync(CancellationToken ct)
    {
        var owners = await _users.ListByRoleAsync(UserRole.Owner, ct);
        return owners.Where(u => u.IsActive).Select(u => new OwnerSummary
        {
            Id       = u.Id,
            FullName = u.FullName,
            Email    = u.Email,
            Phone    = u.Phone ?? ""
        });
    }

    public async Task<IEnumerable<OwnerSummary>> ListVeterinariansAsync(CancellationToken ct)
    {
        var vets = await _users.ListByRoleAsync(UserRole.Veterinarian, ct);
        return vets.Where(u => u.IsActive).Select(u => new OwnerSummary
        {
            Id       = u.Id,
            FullName = u.FullName,
            Email    = u.Email,
            Phone    = u.Phone ?? ""
        });
    }

    // ── Admin ────────────────────────────────────────────────────────────────────

    public async Task<AdminPagedUsers> AdminListUsersAsync(
        string? role, string? search, bool? isActive, int page, int pageSize, CancellationToken ct)
    {
        var (data, total) = await _users.ListAllAsync(role, search, isActive, page, pageSize, ct);
        return new AdminPagedUsers
        {
            Items      = data.Select(MapToAdminItem),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<AdminUserItem> AdminGetUserAsync(Guid id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Usuario no encontrado.");
        return MapToAdminItem(user);
    }

    public async Task<AdminUserItem> AdminCreateUserAsync(AdminCreateUserRequest req, CancellationToken ct)
    {
        if (await _users.EmailExistsAsync(req.Email, ct))
            throw new ConflictException("El correo ya está registrado.");

        ValidatePasswordStrength(req.Password);

        if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var role))
            throw new ValidationException("Rol inválido. Valores permitidos: Veterinarian, Owner, Admin.");

        var user = User.Create(
            req.FirstName, req.LastName,
            req.Email, _hasher.Hash(req.Password),
            req.Phone, role);

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);
        return MapToAdminItem(user);
    }

    public async Task<AdminUserItem> AdminUpdateUserAsync(Guid id, AdminUpdateUserRequest req, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Usuario no encontrado.");

        if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var role))
            throw new ValidationException("Rol inválido.");

        user.AdminUpdate(req.FirstName, req.LastName, req.Phone, role);
        await _users.UpdateAsync(user, ct);
        await _users.SaveChangesAsync(ct);
        return MapToAdminItem(user);
    }

    public async Task AdminSetActiveAsync(Guid id, bool active, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Usuario no encontrado.");

        // El administrador principal (primer Admin) no puede desactivarse a sí mismo desde la API
        user.SetActive(active);
        await _users.UpdateAsync(user, ct);
        await _users.SaveChangesAsync(ct);
    }

    public async Task AdminResetPasswordAsync(Guid id, string newPassword, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Usuario no encontrado.");

        ValidatePasswordStrength(newPassword);
        user.ChangePassword(_hasher.Hash(newPassword));
        await _users.UpdateAsync(user, ct);
        await _users.SaveChangesAsync(ct);
    }

    // Aplica las mismas reglas que el frontend para que el backend sea la fuente de verdad
    private static void ValidatePasswordStrength(string password)
    {
        if (password.Length < 8)
            throw new ValidationException("La contraseña debe tener al menos 8 caracteres.");
        if (!password.Any(char.IsUpper))
            throw new ValidationException("La contraseña debe incluir al menos una mayúscula.");
        if (!password.Any(char.IsDigit))
            throw new ValidationException("La contraseña debe incluir al menos un número.");
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            throw new ValidationException("La contraseña debe incluir al menos un carácter especial.");
    }

    private static AdminUserItem MapToAdminItem(User u) => new()
    {
        Id        = u.Id,
        FirstName = u.FirstName,
        LastName  = u.LastName,
        Email     = u.Email,
        Phone     = u.Phone ?? "",
        Role      = u.Role.ToString().ToLowerInvariant(),
        IsActive  = u.IsActive,
        CreatedAt = u.CreatedAt
    };

    private static UserResponse MapToUserResponse(User user) => new()
    {
        Id        = user.Id,
        FirstName = user.FirstName,
        LastName  = user.LastName,
        Email     = user.Email,
        Phone     = user.Phone,
        Role      = user.Role.ToString().ToLowerInvariant(),
        CreatedAt = user.CreatedAt
    };
}
