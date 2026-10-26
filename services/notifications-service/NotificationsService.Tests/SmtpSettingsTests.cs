using MailKit.Security;
using Microsoft.Extensions.Configuration;
using NotificationsService.Infrastructure.Services;

namespace NotificationsService.Tests;

public class SmtpSettingsTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void FromConfiguration_WithoutSecurity_DefaultsToStartTls()
    {
        var settings = SmtpSettings.FromConfiguration(Config(new() { ["Smtp:User"] = "mailer" }));

        Assert.Equal(SecureSocketOptions.StartTls, settings.Security);
        Assert.True(settings.RequiresAuthentication);
    }

    [Theory]
    [InlineData("None", SecureSocketOptions.None)]
    [InlineData("starttls", SecureSocketOptions.StartTls)]
    [InlineData("SslOnConnect", SecureSocketOptions.SslOnConnect)]
    public void FromConfiguration_ParsesSecurityIgnoringCase(string raw, SecureSocketOptions expected)
    {
        var settings = SmtpSettings.FromConfiguration(Config(new() { ["Smtp:Security"] = raw }));

        Assert.Equal(expected, settings.Security);
    }

    [Theory]
    [InlineData("tls-please")]
    [InlineData("99")]
    public void FromConfiguration_InvalidSecurity_Throws(string raw)
    {
        Assert.Throws<InvalidOperationException>(() =>
            SmtpSettings.FromConfiguration(Config(new() { ["Smtp:Security"] = raw })));
    }

    [Fact]
    public void FromConfiguration_WithoutUser_SkipsAuthentication()
    {
        var settings = SmtpSettings.FromConfiguration(Config(new() { ["Smtp:Host"] = "mailpit" }));

        Assert.False(settings.RequiresAuthentication);
    }
}
