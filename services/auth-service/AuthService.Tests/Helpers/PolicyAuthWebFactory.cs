namespace AuthService.Tests.Helpers;

// Base en memoria propia para que los usuarios creados por estas pruebas no se mezclen con los de AuthControllerTests
public class PolicyAuthWebFactory : AuthWebFactory
{
    protected override string DatabaseName => "AuthPolicyTestDb";
}
