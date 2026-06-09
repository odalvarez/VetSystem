using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;

namespace AuthService.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public UserRole Role     { get; private set; }
    public bool     IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private User() { }

    public static User Create(
        string firstName, string lastName,
        string email, string passwordHash,
        string phone, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("El correo es obligatorio.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("El hash de contraseña es obligatorio.");

        return new User
        {
            Id           = Guid.NewGuid(),
            FirstName    = firstName.Trim(),
            LastName     = lastName.Trim(),
            Email        = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Phone        = phone.Trim(),
            Role         = role,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string firstName, string lastName, string phone)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("El nuevo hash de contraseña es obligatorio.");

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool active)
    {
        IsActive  = active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRole(UserRole role)
    {
        Role      = role;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AdminUpdate(string firstName, string lastName, string phone, UserRole role)
    {
        FirstName = firstName.Trim();
        LastName  = lastName.Trim();
        Phone     = phone.Trim();
        Role      = role;
        UpdatedAt = DateTime.UtcNow;
    }

    public string FullName => $"{FirstName} {LastName}";
}
