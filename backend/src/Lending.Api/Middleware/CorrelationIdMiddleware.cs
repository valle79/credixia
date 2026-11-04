using System.Diagnostics;
using System.Text.RegularExpressions;
using Serilog.Context;

namespace Lending.Api.Middleware;

/// <summary>
/// Accepts a client correlation identifier only when it is short and safe; otherwise
/// generates a new one. The value is echoed in the response and pushed to structured logs.
/// </summary>
public sealed partial class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headerValue = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = !string.IsNullOrWhiteSpace(headerValue) && SafeCorrelationId().IsMatch(headerValue)
            ? headerValue
            : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._:-]{1,100}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 500)]
    private static partial Regex SafeCorrelationId();
}
