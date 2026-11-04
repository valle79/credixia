using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Lending.Domain.Identity;
using Lending.Domain.Tenancy;
using Lending.Infrastructure.Persistence;
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
public sealed class AuthFlowTests : IDisposable
{
    private const string Password = "Sup3rSecret!Pass";
    private readonly PostgresFixture _fixture;
    private readonly Pbkdf2PasswordHasher _passwordHasher = new();
    private readonly WebApplicationFactory<Program> _factory;

    public AuthFlowTests(PostgresFixture fixture)
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
    public async Task Platform_admin_can_log_in_without_a_tenant_and_manage_tenants()
    {
        var email = NewEmail();
        await SeedUserAsync(email, isPlatformAdmin: true);

        using var client = CreateClient();
        using var login = await PostJsonAsync(
            client,
            "/api/v1/auth/login",
            new { email, password = Password, tenantSlug = (string?)null });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginPayload = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.False(loginPayload.RootElement.GetProperty("requiresTenantSelection").GetBoolean());

        var session = loginPayload.RootElement.GetProperty("session");
        var accessToken = session.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.Equal(JsonValueKind.Null, session.GetProperty("tenant").ValueKind);
        Assert.Contains(
            session.GetProperty("permissions").EnumerateArray().Select(item => item.GetString()),
            permission => permission == Permissions.PlatformTenantsManage);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var slug = NewSlug();
        using var created = await PostJsonAsync(
            client,
            "/api/v1/platform/tenants",
            new { slug = slug.Value, name = "Acme Capital", ownerEmail = NewEmail() });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdPayload = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        Assert.Equal(slug.Value, createdPayload.RootElement.GetProperty("slug").GetString());

        using var tenants = await client.GetAsync("/api/v1/platform/tenants");
        Assert.Equal(HttpStatusCode.OK, tenants.StatusCode);
        using var tenantsPayload = JsonDocument.Parse(await tenants.Content.ReadAsStringAsync());
        Assert.Contains(
            tenantsPayload.RootElement.EnumerateArray(),
            item => item.GetProperty("slug").GetString() == slug.Value);
    }

    [Fact]
    public async Task Tenant_owner_cannot_create_or_manage_platform_tenants()
    {
        var email = NewEmail();
        var seeded = await SeedTenantForUserAsync(email, RoleCodes.Owner);

        using var client = CreateClient();
        using var login = await PostJsonAsync(
            client,
            "/api/v1/auth/login",
            new { email, password = Password, tenantSlug = seeded.TenantSlug });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginPayload = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var accessToken = loginPayload.RootElement
            .GetProperty("session")
            .GetProperty("accessToken")
            .GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var list = await client.GetAsync("/api/v1/platform/tenants");
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);

        using var created = await PostJsonAsync(
            client,
            "/api/v1/platform/tenants",
            new { slug = NewSlug().Value, name = "Otra Organización", ownerEmail = NewEmail() });
        Assert.Equal(HttpStatusCode.Forbidden, created.StatusCode);

        using var status = await client.PatchAsJsonAsync(
            $"/api/v1/platform/tenants/{seeded.TenantId}/status",
            new { suspend = true });
        Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
    }

    [Fact]
    public async Task Owner_logs_in_to_tenant_and_receives_tenant_scoped_session()
    {
        var email = NewEmail();
        var seeded = await SeedTenantForUserAsync(email, RoleCodes.Owner);

        using var client = CreateClient();
        using var login = await PostJsonAsync(
            client,
            "/api/v1/auth/login",
            new { email, password = Password, tenantSlug = seeded.TenantSlug });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginPayload = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var session = loginPayload.RootElement.GetProperty("session");
        var accessToken = session.GetProperty("accessToken").GetString();

        Assert.Equal(seeded.TenantId, session.GetProperty("tenant").GetProperty("id").GetGuid());
        Assert.Contains(
            session.GetProperty("permissions").EnumerateArray().Select(item => item.GetString()),
            permission => permission == Permissions.SettingsManage);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var mePayload = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.Equal(email, mePayload.RootElement.GetProperty("user").GetProperty("email").GetString());

        using var currentTenant = await client.GetAsync("/api/v1/tenants/current");
        Assert.Equal(HttpStatusCode.OK, currentTenant.StatusCode);
        using var currentTenantPayload = JsonDocument.Parse(
            await currentTenant.Content.ReadAsStringAsync());
        Assert.Equal(
            seeded.TenantSlug,
            currentTenantPayload.RootElement.GetProperty("tenant").GetProperty("slug").GetString());
    }

    [Fact]
    public async Task Refresh_rotates_the_cookie_and_reuse_revokes_the_family()
    {
        var email = NewEmail();
        var seeded = await SeedTenantForUserAsync(email, RoleCodes.Owner);

        using var client = CreateClient();
        using var login = await PostJsonAsync(
            client,
            "/api/v1/auth/login",
            new { email, password = Password, tenantSlug = seeded.TenantSlug });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var originalCookie = GetRefreshCookie(login);
        Assert.False(string.IsNullOrWhiteSpace(originalCookie));

        using var refreshed = await client.PostAsync("/api/v1/auth/refresh", content: null);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var rotatedCookie = GetRefreshCookie(refreshed);
        Assert.False(string.IsNullOrWhiteSpace(rotatedCookie));
        Assert.NotEqual(originalCookie, rotatedCookie);

        using var reuseClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
        });

        using var reused = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        reused.Headers.Add("Cookie", originalCookie);
        using var reuseResponse = await reuseClient.SendAsync(reused);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        using var rotatedAttempt = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        rotatedAttempt.Headers.Add("Cookie", rotatedCookie ?? string.Empty);
        using var rotatedResponse = await reuseClient.SendAsync(rotatedAttempt);
        Assert.Equal(HttpStatusCode.Unauthorized, rotatedResponse.StatusCode);
    }

    [Fact]
    public async Task Login_with_multiple_memberships_requires_tenant_selection()
    {
        var email = NewEmail();
        await SeedUserAsync(email, isPlatformAdmin: false);
        var first = await SeedTenantForUserAsync(email, RoleCodes.Owner);
        await SeedTenantForUserAsync(email, RoleCodes.Cashier);

        using var client = CreateClient();
        using var login = await PostJsonAsync(
            client,
            "/api/v1/auth/login",
            new { email, password = Password, tenantSlug = (string?)null });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var payload = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.True(payload.RootElement.GetProperty("requiresTenantSelection").GetBoolean());
        Assert.Equal(JsonValueKind.Null, payload.RootElement.GetProperty("session").ValueKind);
        Assert.Contains(
            payload.RootElement.GetProperty("tenants").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == first.TenantId);
    }

    [Fact]
    public async Task Wrong_password_returns_unauthorized_without_a_session()
    {
        var email = NewEmail();
        await SeedTenantForUserAsync(email, RoleCodes.Owner);

        using var client = CreateClient();
        using var login = await PostJsonAsync(
            client,
            "/api/v1/auth/login",
            new { email, password = "incorrect-password", tenantSlug = (string?)null });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Null(login.Headers.TryGetValues("Set-Cookie", out _) ? "cookie" : null);
    }

    public void Dispose() => _factory.Dispose();

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });

    private async Task SeedUserAsync(string email, bool isPlatformAdmin)
    {
        await using var context = _fixture.CreateContext();
        var now = DateTimeOffset.UtcNow;
        var user = User.Create(
            Email.Create(email),
            "Test User",
            _passwordHasher.Hash(Password),
            now,
            isPlatformAdmin);
        user.VerifyEmail(now);
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    private async Task<SeededTenant> SeedTenantForUserAsync(string email, string roleCode)
    {
        await using var context = _fixture.CreateContext();
        var user = await context.Users.SingleOrDefaultAsync(item => item.Email == Email.Create(email));
        if (user is null)
        {
            var now = DateTimeOffset.UtcNow;
            user = User.Create(
                Email.Create(email),
                "Test User",
                _passwordHasher.Hash(Password),
                now,
                isPlatformAdmin: false);
            user.VerifyEmail(now);
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        return await SeedTenantAsync(context, user, roleCode);
    }

    private static async Task<SeededTenant> SeedTenantAsync(
        LendingDbContext context,
        User user,
        string roleCode)
    {
        var now = DateTimeOffset.UtcNow;
        var slug = NewSlug();
        var tenant = Tenant.Create(slug, $"Acme {slug.Value}", now);
        context.Tenants.Add(tenant);
        context.TenantSettings.Add(TenantSettings.CreateDefault(tenant.Id, now));

        var roles = SystemRoles.All
            .Select(definition => Role.CreateForTenant(tenant.Id, definition, now))
            .ToList();
        context.Roles.AddRange(roles);

        var roleId = roles.Single(role => role.Code == roleCode).Id;
        context.TenantMemberships.Add(
            TenantMembership.CreateActive(tenant.Id, user.Id, [roleId], now));

        await context.SaveChangesAsync();
        return new SeededTenant(tenant.Id, slug.Value);
    }

    private static async Task<HttpResponseMessage> PostJsonAsync(
        HttpClient client,
        string path,
        object body) =>
        await client.PostAsJsonAsync(path, body);

    private static string? GetRefreshCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        return cookies
            .Select(cookie => cookie.Split(';', 2)[0])
            .SingleOrDefault(cookie => cookie.StartsWith("lending_refresh=", StringComparison.Ordinal));
    }

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.test";

    private static TenantSlug NewSlug() =>
        TenantSlug.Create("t-" + Guid.NewGuid().ToString("N")[..12]);

    private sealed record SeededTenant(Guid TenantId, string TenantSlug);
}
