using AuthService.Application.DTOs;
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

        if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var role))
            throw new ValidationException("El rol debe ser 'veterinarian' o 'owner'.");

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

        return new LoginResponse
        {
            AccessToken = _jwt.GenerateToken(user),
            ExpiresIn   = _jwt.ExpiresInSeconds,
            User = new LoginUserInfo
            {
                Id       = user.Id,
                Email    = user.Email,
                FullName = user.FullName,
                Role     = user.Role.ToString().ToLowerInvariant()
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
