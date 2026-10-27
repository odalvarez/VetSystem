using System.Text;

namespace AuthService.Application.Security;

// Única fuente de verdad de la política de contraseñas. El frontend la replica en PasswordStrength.razor
// y ambas implementaciones se validan contra tests/password-cases.json.
public static class PasswordPolicy
{
    public const int MinLength = 8;

    // BCrypt solo usa los primeros 72 bytes: dos contraseñas que comparten ese prefijo serían la misma
    public const int MaxBytes = 72;

    public static IReadOnlyList<string> Validate(string? password)
    {
        password ??= "";
        var errors = new List<string>();

        if (password.Length < MinLength)
            errors.Add($"La contraseña debe tener al menos {MinLength} caracteres.");
        if (Encoding.UTF8.GetByteCount(password) > MaxBytes)
            errors.Add("La contraseña no puede superar los 72 caracteres (las letras con tilde cuentan doble).");
        if (!password.Any(char.IsUpper))
            errors.Add("La contraseña debe incluir al menos una mayúscula.");
        if (!password.Any(char.IsDigit))
            errors.Add("La contraseña debe incluir al menos un número.");
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            errors.Add("La contraseña debe incluir al menos un carácter especial.");

        return errors;
    }

    public static bool IsValid(string? password) => Validate(password).Count == 0;
}
