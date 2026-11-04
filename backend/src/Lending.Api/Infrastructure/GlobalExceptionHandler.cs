using Lending.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Lending.Api.Infrastructure;

/// <summary>
/// Central error boundary. Domain messages are safe to expose; unexpected exceptions are
/// logged and replaced with a generic problem response.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var (statusCode, title, code, detail) = exception switch
        {
            DomainException domainException => (
                StatusCodes.Status400BadRequest,
                "Business rule violated",
                domainException.Code,
                domainException.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                "server.unexpected_error",
                "An unexpected error occurred. Use the trace identifier when contacting support."),
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path} with correlation {CorrelationId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                "Business rule {Code} rejected {Method} {Path} with correlation {CorrelationId}",
                code,
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
        };

        problemDetails.Extensions["code"] = code;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
