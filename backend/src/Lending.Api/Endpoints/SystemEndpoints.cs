using System.Reflection;

namespace Lending.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1")
            .WithTags("System");

        group.MapGet("/ping", (IHostEnvironment environment) =>
        {
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "unknown";

            return TypedResults.Ok(new PingResponse(
                Status: "ok",
                Service: "lending-api",
                Environment: environment.EnvironmentName,
                Version: version,
                Utc: DateTimeOffset.UtcNow));
        })
        .WithName("Ping")
        .WithSummary("Liveness probe for the API process")
        .Produces<PingResponse>()
        .AllowAnonymous();

        return endpoints;
    }

    public sealed record PingResponse(
        string Status,
        string Service,
        string Environment,
        string Version,
        DateTimeOffset Utc);
}
