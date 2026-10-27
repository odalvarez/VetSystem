using Microsoft.AspNetCore.Mvc;

namespace PatientsService.API;

// Los errores de validación salen con la misma forma que los del ErrorHandlingMiddleware (detail en español
// listo para mostrar) más el detalle por campo; así el frontend trata todo 400 igual.
public static class ValidationErrorResponse
{
    // Los errores de deserialización (JSON mal formado, tipos incompatibles) traen mensajes técnicos en inglés
    private const string MalformedInput = "Los datos enviados no tienen el formato esperado.";

    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors.Select(x => IsTechnical(x) ? MalformedInput : x.ErrorMessage).Distinct().ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Type     = "https://httpstatuses.com/400",
            Title    = "Validación fallida",
            Status   = StatusCodes.Status400BadRequest,
            Detail   = string.Join(" ", errors.Values.SelectMany(m => m).Distinct()),
            Instance = context.HttpContext.Request.Path
        };

        // JsonResult fija el content-type; con ObjectResult la negociación podía devolver application/json
        return new JsonResult(problem)
        {
            StatusCode  = StatusCodes.Status400BadRequest,
            ContentType = "application/problem+json"
        };
    }

    private static bool IsTechnical(Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error) =>
        error.Exception != null || string.IsNullOrEmpty(error.ErrorMessage) || error.ErrorMessage.StartsWith("The ");
}
