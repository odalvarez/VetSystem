using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace NotificationsService.API;

// Asigna mensajes en español a los atributos de validación que no traen uno propio, con el nombre del campo
// en español. Así los DTO no repiten un ErrorMessage por atributo y todos los mensajes siguen el mismo patrón.
public sealed class SpanishValidationMessages : IValidationMetadataProvider
{
    private static readonly Dictionary<string, string> FieldNames = new()
    {
        ["AppointmentId"] = "cita",
        ["Body"         ] = "cuerpo",
        ["Channels"     ] = "canales",
        ["Message"      ] = "mensaje",
        ["OwnerEmail"   ] = "correo del propietario",
        ["OwnerName"    ] = "nombre del propietario",
        ["OwnerPhone"   ] = "teléfono del propietario",
        ["PatientName"  ] = "nombre de la mascota",
        ["ScheduledAt"  ] = "fecha y hora",
        ["Subject"      ] = "asunto",
        ["To"           ] = "destinatario"
    };

    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        var name  = context.Key.Name ?? "";
        var field = FieldNames.TryGetValue(name, out var es) ? es : name;

        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (attribute.ErrorMessage != null || attribute.ErrorMessageResourceType != null)
                continue;

            attribute.ErrorMessage = attribute switch
            {
                RequiredAttribute       => $"El campo {field} es obligatorio.",
                MaxLengthAttribute m    => $"El campo {field} no puede superar {m.Length} caracteres.",
                MinLengthAttribute m    => $"El campo {field} debe tener al menos {m.Length} caracteres.",
                StringLengthAttribute s => $"El campo {field} no puede superar {s.MaximumLength} caracteres.",
                RangeAttribute r        => $"El campo {field} debe estar entre {r.Minimum} y {r.Maximum}.",
                EmailAddressAttribute   => $"El campo {field} no tiene un formato de correo válido.",
                _                       => $"El campo {field} no es válido."
            };
        }
    }
}
