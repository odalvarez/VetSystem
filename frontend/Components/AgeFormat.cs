namespace VetSystem.Frontend.Components;

public static class AgeFormat
{
    // Edad aproximada en meses (menos de un año) o en años, calculada desde la fecha de nacimiento
    public static string FromBirthDate(DateOnly birthDate)
    {
        var today  = DateOnly.FromDateTime(DateTime.Today);
        var months = (today.Year - birthDate.Year) * 12 + (today.Month - birthDate.Month);
        if (today.Day < birthDate.Day) months--;
        months = Math.Max(0, months);
        if (months < 12) return months == 1 ? "1 mes" : $"{months} meses";
        var years = months / 12;
        return years == 1 ? "1 año" : $"{years} años";
    }
}
