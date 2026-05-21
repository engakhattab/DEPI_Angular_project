namespace HR.Shared.Results;

public record ServiceError
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public ErrorType Type { get; init; }

    public enum ErrorType
    {
        NotFound,
        Conflict,
        Validation,
        Unauthorized,
        Forbidden,
        Internal,
        BusinessRule
    }

    public static ServiceError NotFound(string message, string code = "NOT_FOUND") =>
        new() { Code = code, Message = message, Type = ErrorType.NotFound };

    public static ServiceError Conflict(string message, string code = "CONFLICT") =>
        new() { Code = code, Message = message, Type = ErrorType.Conflict };

    public static ServiceError Validation(string message, string code = "VALIDATION_ERROR") =>
        new() { Code = code, Message = message, Type = ErrorType.Validation };

    public static ServiceError Unauthorized(string message = "Unauthorized", string code = "UNAUTHORIZED") =>
        new() { Code = code, Message = message, Type = ErrorType.Unauthorized };

    public static ServiceError Forbidden(string message = "Forbidden", string code = "FORBIDDEN") =>
        new() { Code = code, Message = message, Type = ErrorType.Forbidden };

    public static ServiceError Internal(string message = "An internal error occurred", string code = "INTERNAL_ERROR") =>
        new() { Code = code, Message = message, Type = ErrorType.Internal };

    public static ServiceError BusinessRule(string message, string code = "BUSINESS_RULE_VIOLATION") =>
        new() { Code = code, Message = message, Type = ErrorType.BusinessRule };
}
