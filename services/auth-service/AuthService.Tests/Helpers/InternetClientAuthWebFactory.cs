namespace AuthService.Tests.Helpers;

// Conexión que llega directo desde Internet, sin pasar por un proxy de confianza
public class InternetClientAuthWebFactory : RateLimitedAuthWebFactory
{
    protected override string DatabaseName => "AuthInternetClientTestDb";
    protected override string RemoteIp     => "198.51.100.7";
}
