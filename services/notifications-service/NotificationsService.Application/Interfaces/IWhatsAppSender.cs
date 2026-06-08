namespace NotificationsService.Application.Interfaces;

public interface IWhatsAppSender
{
    Task SendAsync(string to, string message, CancellationToken ct = default);
}
