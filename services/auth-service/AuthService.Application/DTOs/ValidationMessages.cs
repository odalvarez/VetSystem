namespace AuthService.Application.DTOs;

// Mensajes de validación en un solo lugar: llegan tal cual al usuario en el campo "detail" del error
public static class ValidationMessages
{
    public const string FirstNameRequired = "El nombre es obligatorio.";
    public const string FirstNameTooLong  = "El nombre no puede superar los 100 caracteres.";
    public const string LastNameRequired  = "El apellido es obligatorio.";
    public const string LastNameTooLong   = "El apellido no puede superar los 100 caracteres.";
    public const string EmailRequired     = "El correo es obligatorio.";
    public const string EmailInvalid      = "El correo no tiene un formato válido.";
    public const string EmailTooLong      = "El correo no puede superar los 256 caracteres.";
    public const string PhoneTooLong      = "El teléfono no puede superar los 20 caracteres.";
    public const string PasswordRequired  = "La contraseña es obligatoria.";
    public const string RoleRequired      = "El rol es obligatorio.";

    public const int NameMaxLength  = 100;
    public const int EmailMaxLength = 256;
    public const int PhoneMaxLength = 20;
}
