using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Lending.Domain.Identity;
using Lending.Domain.Tenancy;
using Lending.Infrastructure.Security;
using Lending.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lending.IntegrationTests.Api;

[Collection(PostgresFixtureDefinition.Name)]
public sealed class CustomersApiTests : IDisposable
{
    private const string Password = "Sup3rSecret!Pass";
    private const string DuplicateDocumentMessage =
        "Este número de documento ya está registrado en tu empresa";
    private const string UnderageMessage =
        "El cliente debe tener al menos 18 años para poder registrarse";

    private readonly PostgresFixture _fixture;
    private readonly Pbkdf2PasswordHasher _passwordHasher = new();
    private readonly WebApplicationFactory<Program> _factory;

    public CustomersApiTests(PostgresFixture fixture)
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
                    ["Database:MigrateOnStartup"] = "false",
                    ["Bootstrap:Email"] = string.Empty,
                    ["Bootstrap:Password"] = string.Empty,
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
    public async Task Duplicate_document_in_same_tenant_returns_conflict_and_safe_message()
    {
        var email = NewEmail();
        var tenant = await SeedTenantForUserAsync(email);
        using var client = await CreateAuthenticatedClientAsync(email, tenant.TenantSlug);

        var documentNumber = NewDocumentNumber();
        using var first = await PostCustomerAsync(client, documentNumber);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var duplicate = await PostCustomerAsync(client, $"  {documentNumber}  ");
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var content = await duplicate.Content.ReadAsStringAsync();
        using var payload = JsonDocument.Parse(content);
        Assert.Equal(
            DuplicateDocumentMessage,
            payload.RootElement.GetProperty("detail").GetString());
        Assert.False(content.Contains("23505", StringComparison.OrdinalIgnoreCase));
        Assert.False(content.Contains("IX_customers", StringComparison.OrdinalIgnoreCase));

        await using var context = _fixture.CreateContext();
        var storedCount = await context.Customers.CountAsync(customer =>
            customer.TenantId == tenant.TenantId &&
            customer.DocumentNumber == documentNumber);

        Assert.Equal(1, storedCount);
    }

    [Fact]
    public async Task Same_document_in_different_tenants_is_allowed()
    {
        var firstEmail = NewEmail();
        var firstTenant = await SeedTenantForUserAsync(firstEmail);
        using var firstClient = await CreateAuthenticatedClientAsync(
            firstEmail,
            firstTenant.TenantSlug);

        var secondEmail = NewEmail();
        var secondTenant = await SeedTenantForUserAsync(secondEmail);
        using var secondClient = await CreateAuthenticatedClientAsync(
            secondEmail,
            secondTenant.TenantSlug);

        var documentNumber = NewDocumentNumber();
        using var first = await PostCustomerAsync(firstClient, documentNumber);
        using var second = await PostCustomerAsync(secondClient, documentNumber);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        await using var context = _fixture.CreateContext();
        var storedTenantIds = await context.Customers
            .Where(customer => customer.DocumentNumber == documentNumber)
            .Select(customer => customer.TenantId)
            .ToListAsync();

        Assert.Equal(2, storedTenantIds.Count);
        Assert.Contains(firstTenant.TenantId, storedTenantIds);
        Assert.Contains(secondTenant.TenantId, storedTenantIds);
    }

    [Fact]
    public async Task Concurrent_duplicate_document_requests_do_not_return_server_error()
    {
        var email = NewEmail();
        var tenant = await SeedTenantForUserAsync(email);
        using var client = await CreateAuthenticatedClientAsync(email, tenant.TenantSlug);

        var documentNumber = NewDocumentNumber();
        var responses = await Task.WhenAll(
            PostCustomerAsync(client, documentNumber),
            PostCustomerAsync(client, documentNumber));

        try
        {
            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
            Assert.All(
                responses,
                response => Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode));
            var conflict = responses.Single(
                response => response.StatusCode == HttpStatusCode.Conflict);

            var content = await conflict.Content.ReadAsStringAsync();
            using var payload = JsonDocument.Parse(content);
            Assert.Equal(
                DuplicateDocumentMessage,
                payload.RootElement.GetProperty("detail").GetString());
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task Check_document_reports_availability_in_tenant_and_ignores_other_tenants()
    {
        var firstEmail = NewEmail();
        var firstTenant = await SeedTenantForUserAsync(firstEmail);
        using var firstClient = await CreateAuthenticatedClientAsync(
            firstEmail,
            firstTenant.TenantSlug);

        var documentNumber = NewDocumentNumber();

        using (var available = await CheckDocumentAsync(firstClient, documentNumber))
        {
            Assert.Equal(HttpStatusCode.OK, available.StatusCode);
            using var payload = JsonDocument.Parse(await available.Content.ReadAsStringAsync());
            Assert.True(payload.RootElement.GetProperty("available").GetBoolean());
        }

        using (var created = await PostCustomerAsync(firstClient, documentNumber))
        {
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }

        using (var duplicate = await CheckDocumentAsync(firstClient, documentNumber))
        {
            Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
            using var payload = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
            Assert.False(payload.RootElement.GetProperty("available").GetBoolean());
            Assert.Equal(
                DuplicateDocumentMessage,
                payload.RootElement.GetProperty("message").GetString());
        }

        var secondEmail = NewEmail();
        var secondTenant = await SeedTenantForUserAsync(secondEmail);
        using var secondClient = await CreateAuthenticatedClientAsync(
            secondEmail,
            secondTenant.TenantSlug);

        using var otherTenant = await CheckDocumentAsync(secondClient, documentNumber);
        Assert.Equal(HttpStatusCode.OK, otherTenant.StatusCode);
        using var otherPayload = JsonDocument.Parse(
            await otherTenant.Content.ReadAsStringAsync());
        Assert.True(otherPayload.RootElement.GetProperty("available").GetBoolean());
    }

    [Fact]
    public async Task Check_document_rejects_invalid_format()
    {
        var email = NewEmail();
        var tenant = await SeedTenantForUserAsync(email);
        using var client = await CreateAuthenticatedClientAsync(email, tenant.TenantSlug);

        using var response = await CheckDocumentAsync(client, "123");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains("DNI", payload.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Underage_birth_date_is_rejected_on_create_and_update()
    {
        var email = NewEmail();
        var tenant = await SeedTenantForUserAsync(email);
        using var client = await CreateAuthenticatedClientAsync(email, tenant.TenantSlug);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using (var underageCreate = await PostCustomerAsync(
            client,
            NewDocumentNumber(),
            today.AddYears(-17)))
        {
            Assert.Equal(HttpStatusCode.BadRequest, underageCreate.StatusCode);
            using var payload = JsonDocument.Parse(
                await underageCreate.Content.ReadAsStringAsync());
            Assert.Equal(
                UnderageMessage,
                payload.RootElement.GetProperty("detail").GetString());
        }

        var documentNumber = NewDocumentNumber();
        var adultBirthDate = today.AddYears(-30);
        string customerId;

        using (var created = await PostCustomerAsync(client, documentNumber, adultBirthDate))
        {
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var payload = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            customerId = payload.RootElement.GetProperty("id").GetString()!;
        }

        using (var underageUpdate = await PutCustomerBirthDateAsync(
            client,
            customerId,
            today.AddYears(-17)))
        {
            Assert.Equal(HttpStatusCode.BadRequest, underageUpdate.StatusCode);
            using var payload = JsonDocument.Parse(
                await underageUpdate.Content.ReadAsStringAsync());
            Assert.Equal(
                UnderageMessage,
                payload.RootElement.GetProperty("detail").GetString());
        }

        await using var context = _fixture.CreateContext();
        var storedBirthDate = await context.Customers
            .Where(customer => customer.TenantId == tenant.TenantId)
            .Where(customer => customer.DocumentNumber == documentNumber)
            .Select(customer => customer.BirthDate)
            .SingleAsync();

        Assert.Equal(adultBirthDate, storedBirthDate);
    }

    public void Dispose() => _factory.Dispose();

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string tenantSlug)
    {
        var client = CreateClient();
        using var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = Password, tenantSlug });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var payload = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var accessToken = payload.RootElement
            .GetProperty("session")
            .GetProperty("accessToken")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }

    private static Task<HttpResponseMessage> PostCustomerAsync(
        HttpClient client,
        string documentNumber,
        DateOnly? birthDate = null) =>
        client.PostAsJsonAsync(
            "/api/v1/customers",
            new
            {
                type = "Natural",
                documentType = "DNI",
                documentNumber,
                fullName = "Cliente de Prueba",
                birthDate,
            });

    private static async Task<HttpResponseMessage> CheckDocumentAsync(
        HttpClient client,
        string documentNumber,
        string documentType = "DNI") =>
        await client.GetAsync(
            $"/api/v1/customers/check-document?documentType={documentType}&documentNumber={documentNumber}");

    private static Task<HttpResponseMessage> PutCustomerBirthDateAsync(
        HttpClient client,
        string customerId,
        DateOnly birthDate) =>
        client.PutAsJsonAsync(
            $"/api/v1/customers/{customerId}",
            new
            {
                fullName = "Cliente de Prueba",
                birthDate,
            });

    private async Task<SeededTenant> SeedTenantForUserAsync(string email)
    {
        await using var context = _fixture.CreateContext();
        var now = DateTimeOffset.UtcNow;
        var user = User.Create(
            Email.Create(email),
            "Test User",
            _passwordHasher.Hash(Password),
            now,
            isPlatformAdmin: false);
        user.VerifyEmail(now);
        context.Users.Add(user);

        var slug = NewSlug();
        var tenant = Tenant.Create(slug, $"Acme {slug.Value}", now);
        context.Tenants.Add(tenant);
        context.TenantSettings.Add(TenantSettings.CreateDefault(tenant.Id, now));

        var roles = SystemRoles.All
            .Select(definition => Role.CreateForTenant(tenant.Id, definition, now))
            .ToList();
        context.Roles.AddRange(roles);

        var ownerRoleId = roles.Single(role => role.Code == RoleCodes.Owner).Id;
        context.TenantMemberships.Add(
            TenantMembership.CreateActive(tenant.Id, user.Id, [ownerRoleId], now));

        await context.SaveChangesAsync();
        return new SeededTenant(tenant.Id, slug.Value);
    }

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.test";

    private static string NewDocumentNumber() =>
        Random.Shared.Next(10_000_000, 99_999_999)
            .ToString(CultureInfo.InvariantCulture);

    private static TenantSlug NewSlug() =>
        TenantSlug.Create("t-" + Guid.NewGuid().ToString("N")[..12]);

    private sealed record SeededTenant(Guid TenantId, string TenantSlug);
}
