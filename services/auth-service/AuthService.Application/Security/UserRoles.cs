using AuthService.Domain.Enums;

namespace AuthService.Application.Security;

public static class UserRoles
{
    // Enum.TryParse acepta números ("7") y los convierte en roles inexistentes que la BD rechaza con un 500;
    // aquí solo se aceptan los nombres definidos.
    public static bool TryParse(string? raw, out UserRole role)
    {
        role = default;
        return !string.IsNullOrWhiteSpace(raw)
            && raw.All(char.IsLetter)
            && Enum.TryParse(raw, ignoreCase: true, out role)
            && Enum.IsDefined(role);
    }
}
