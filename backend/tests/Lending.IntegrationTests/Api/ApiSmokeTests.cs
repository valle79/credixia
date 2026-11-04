using System.Net;
using System.Text.Json;
using Lending.Api.Middleware;
using Lending.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lending.IntegrationTests.Api;

[Collection(PostgresFixtureDefinition.Name)]
public sealed class ApiSmokeTests : IDisposable
{
    private readonly PostgresFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;

    public ApiSmokeTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] = _fixture.ConnectionString,
                    ["Api:ExposeOpenApi"] = "false",
                    ["OpenTelemetry:OtlpEndpoint"] = string.Empty,
                });
            });
            builder.ConfigureServices(services =>
            {
                foreach (var descriptor in services
                    .Where(item => item.ServiceType == typeof(NpgsqlDataSource))
                    .ToList())
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton<NpgsqlDataSource>(
                    _ => NpgsqlDataSource.Create(_fixture.ConnectionString));
            });
        });
    }

    [Fact]
    public async Task Ping_returns_service_metadata_and_echoes_correlation_id()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/ping");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "test-correlation-1");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "test-correlation-1",
            response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", payload.RootElement.GetProperty("status").GetString());
        Assert.Equal("lending-api", payload.RootElement.GetProperty("service").GetString());
        Assert.Equal("Development", payload.RootElement.GetProperty("environment").GetString());
    }

    [Fact]
    public async Task Liveness_probe_does_not_depend_on_database()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Readiness_probe_reports_postgres_healthy()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unknown_route_returns_problem_details()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, payload.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(
            payload.RootElement.GetProperty("title").GetString()));
    }

    public void Dispose() => _factory.Dispose();
}

