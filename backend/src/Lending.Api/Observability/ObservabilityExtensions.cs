using System.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Lending.Api.Observability;

public static class ObservabilityExtensions
{
    public const string ActivitySourceName = "Lending";

    public static IServiceCollection AddLendingObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var endpointValue = configuration["OpenTelemetry:OtlpEndpoint"];
        Uri? otlpEndpoint = Uri.TryCreate(endpointValue, UriKind.Absolute, out var parsed)
            ? parsed
            : null;

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("lending-api"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(ActivitySourceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (otlpEndpoint is not null)
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = otlpEndpoint);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (otlpEndpoint is not null)
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = otlpEndpoint);
                }
            });

        return services;
    }
}

public static class LendingDiagnostics
{
    public static readonly ActivitySource ActivitySource = new(ObservabilityExtensions.ActivitySourceName);
}
