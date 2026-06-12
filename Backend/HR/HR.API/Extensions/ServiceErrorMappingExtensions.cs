using HR.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace HR.API.Extensions;

public static class ServiceErrorMappingExtensions
{
    public static ActionResult ToActionResult(this ControllerBase controller, ServiceError error)
    {
        var payload = new
        {
            code = error.Code,
            message = error.Message
        };

        return error.Type switch
        {
            _ when error.Code == "PAYLOAD_TOO_LARGE" => controller.StatusCode(StatusCodes.Status413PayloadTooLarge, payload),
            ServiceError.ErrorType.NotFound => controller.NotFound(payload),
            ServiceError.ErrorType.Conflict => controller.Conflict(payload),
            ServiceError.ErrorType.Validation => controller.BadRequest(payload),
            ServiceError.ErrorType.Unauthorized => controller.Unauthorized(payload),
            ServiceError.ErrorType.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, payload),
            ServiceError.ErrorType.BusinessRule => controller.UnprocessableEntity(payload),
            ServiceError.ErrorType.Internal => controller.StatusCode(StatusCodes.Status500InternalServerError, payload),
            _ => controller.BadRequest(payload)
        };
    }
}
