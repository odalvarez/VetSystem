using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using NotificationsService.Application.Interfaces;

namespace NotificationsService.Infrastructure.Services;

public sealed record SmtpSettings(
    string Host, int Port, string User, string Password, string From, SecureSocketOptions Security)
{
    // Sin usuario no se autentica: así funciona contra el SMTP local de las pruebas E2E
    public bool RequiresAuthentication => User.Length > 0;

    public static SmtpSettings FromConfiguration(IConfiguration config)
    {
        var rawSecurity = config["Smtp:Security"];
        var security = SecureSocketOptions.StartTls;

        // Un valor mal escrito no debe degradar en silencio la seguridad del envío
        if (!string.IsNullOrWhiteSpace(rawSecurity) &&
            (!Enum.TryParse(rawSecurity, ignoreCase: true, out security) || !Enum.IsDefined(security)))
            throw new InvalidOperationException($"Smtp:Security inválido: '{rawSecurity}'.");

        return new SmtpSettings(
            Host:     config["Smtp:Host"]     ?? "localhost",
            Port:     int.Parse(config["Smtp:Port"] ?? "587"),
            User:     config["Smtp:User"]     ?? "",
            Password: config["Smtp:Password"] ?? "",
            From:     config["Smtp:From"]     ?? "noreply@vetsystem.co",
            Security: security);
    }
}

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;

    public SmtpEmailSender(SmtpSettings settings) => _settings = settings;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(_settings.From));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body    = new TextPart("html") { Text = htmlBody };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settings.Host, _settings.Port, _settings.Security, ct);
        if (_settings.RequiresAuthentication)
            await smtp.AuthenticateAsync(_settings.User, _settings.Password, ct);
        await smtp.SendAsync(msg, ct);
        await smtp.DisconnectAsync(true, ct);
    }
}
