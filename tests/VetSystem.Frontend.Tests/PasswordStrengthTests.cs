using System.Text.Json;
using Bunit;
using VetSystem.Frontend.Components;

namespace VetSystem.Frontend.Tests;

public class PasswordStrengthTests : TestContext
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
    public void IsValid_MatchesSharedCasesWithBackend(string password, bool expected, string description)
    {
        Assert.True(PasswordStrength.IsValid(password) == expected, description);
    }

    [Fact]
    public void Renders_FourRulesAlwaysVisible()
    {
        var cut = RenderComponent<PasswordStrength>(p => p.Add(x => x.Password, ""));

        Assert.Equal(4, cut.FindAll("li.pwd-rule").Count);
        Assert.Empty(cut.FindAll("li.met"));
    }

    [Fact]
    public void MarksMetRules_AndAnnouncesStateToScreenReaders()
    {
        var cut = RenderComponent<PasswordStrength>(p => p.Add(x => x.Password, "Abcdefg1!"));

        Assert.Equal(4, cut.FindAll("li.met").Count);
        Assert.DoesNotContain("(pendiente)", cut.Markup);
        Assert.Contains("(cumplido)", cut.Markup);
    }

    [Fact]
    public void WarnsAboveSeventyTwoBytes_CountingAccentsDouble()
    {
        var password = "Aa1!" + new string('á', 40); // 4 + 80 bytes

        var cut = RenderComponent<PasswordStrength>(p => p.Add(x => x.Password, password));

        Assert.Contains("Máximo 72 caracteres", cut.Markup);
        Assert.False(PasswordStrength.IsValid(password));
    }
}
