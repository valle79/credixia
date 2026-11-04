namespace Lending.Application.Common;

/// <summary>
/// Application-level error with a stable code that is safe to expose to clients.
/// </summary>
public sealed record AppError(string Code, string Message, ErrorType Type)
{
    public static AppError Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    public static AppError NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static AppError Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static AppError Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    public static AppError Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);

    public static AppError Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);
}
