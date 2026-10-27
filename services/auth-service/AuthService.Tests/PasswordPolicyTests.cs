using System.Text.Json;
using AuthService.Application.Security;

namespace AuthService.Tests;

public class PasswordPolicyTests
{
    public static IEnumerable<object[]> SharedCases()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "password-cases.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var c in doc.RootElement.EnumerateArray())
            yield return [c.GetProperty("password").GetString()!, c.GetProperty("valid").GetBoolean(), c.GetProperty("case").GetString()!];
    }

    [Theory]
    [MemberData(nameof(SharedCases))]
    public void IsValid_MatchesSharedCases(string password, bool expected, string description)
    {
        Assert.True(PasswordPolicy.IsValid(password) == expected, description);
    }

    [Fact]
    public void Validate_ListsEveryBrokenRule()
    {
        var errors = PasswordPolicy.Validate("abc");

        Assert.Contains(errors, e => e.Contains("8 caracteres"));
        Assert.Contains(errors, e => e.Contains("mayúscula"));
        Assert.Contains(errors, e => e.Contains("número"));
        Assert.Contains(errors, e => e.Contains("carácter especial"));
    }

    [Fact]
    public void Validate_Null_IsInvalid()
    {
        Assert.NotEmpty(PasswordPolicy.Validate(null));
    }
}
