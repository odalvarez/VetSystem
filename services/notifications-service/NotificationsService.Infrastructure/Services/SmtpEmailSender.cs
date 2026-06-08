using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using NotificationsService.Application.Interfaces;

namespace NotificationsService.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly string _host;
    private readonly int    _port;
    private readonly string _user;
    private readonly string _pass;
    private readonly string _from;

    public SmtpEmailSender(IConfiguration config)
    {
        _host = config["Smtp:Host"]     ?? "localhost";
        _port = int.Parse(config["Smtp:Port"] ?? "587");
        _user = config["Smtp:User"]     ?? "";
        _pass = config["Smtp:Password"] ?? "";
        _from = config["Smtp:From"]     ?? "noreply@vetsystem.co";
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(_from));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body    = new TextPart("html") { Text = htmlBody };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_host, _port, SecureSocketOptions.StartTls, ct);
        await smtp.AuthenticateAsync(_user, _pass, ct);
        await smtp.SendAsync(msg, ct);
        await smtp.DisconnectAsync(true, ct);
    }
}
