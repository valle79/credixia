using Lending.Domain.Tenancy;
using Lending.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lending.IntegrationTests.Persistence;

[Collection(PostgresFixtureDefinition.Name)]
public sealed class TenancyPersistenceTests
{
    private readonly PostgresFixture _fixture;

    public TenancyPersistenceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migration_creates_tenants_table_and_round_trips_tenant()
    {
        await using var context = _fixture.CreateContext();

        var migrations = await context.Database.GetAppliedMigrationsAsync();
        Assert.Contains(migrations, name =>
            name.EndsWith("InitialTenancy", StringComparison.Ordinal));

        var slug = NewSlug();
        var tenant = Tenant.Create(slug, "  Acme SAC  ", DateTimeOffset.UtcNow);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.Tenants.SingleAsync(item => item.Slug == slug);

        Assert.Equal(tenant.Id, stored.Id);
        Assert.Equal("Acme SAC", stored.Name);
        Assert.Equal(TenantStatus.Active, stored.Status);
    }

    [Fact]
    public async Task Update_round_trips_with_concurrency_token()
    {
        var slug = NewSlug();
        await using (var context = _fixture.CreateContext())
        {
            context.Tenants.Add(Tenant.Create(slug, "Acme", DateTimeOffset.UtcNow));
            await context.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var tenant = await context.Tenants.SingleAsync(item => item.Slug == slug);
            tenant.Rename("Acme Capital", DateTimeOffset.UtcNow.AddMinutes(5));
            await context.SaveChangesAsync();
        }

        await using var verification = _fixture.CreateContext();
        var stored = await verification.Tenants.SingleAsync(item => item.Slug == slug);

        Assert.Equal("Acme Capital", stored.Name);
    }

    [Fact]
    public async Task Status_is_stored_as_readable_string()
    {
        var slug = NewSlug();
        await using (var context = _fixture.CreateContext())
        {
            context.Tenants.Add(Tenant.Create(slug, "Acme", DateTimeOffset.UtcNow));
            await context.SaveChangesAsync();
        }

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT status FROM tenants WHERE slug = @slug", connection);
        command.Parameters.AddWithValue("slug", slug.Value);

        Assert.Equal("Active", await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Duplicate_slug_violates_unique_index()
    {
        var slug = NewSlug();
        await using (var context = _fixture.CreateContext())
        {
            context.Tenants.Add(Tenant.Create(slug, "Acme", DateTimeOffset.UtcNow));
            await context.SaveChangesAsync();
        }

        await using var duplicateContext = _fixture.CreateContext();
        duplicateContext.Tenants.Add(Tenant.Create(slug, "Other", DateTimeOffset.UtcNow));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => duplicateContext.SaveChangesAsync());

        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
        Assert.Equal("ux_tenants_slug", postgresException.ConstraintName);
    }

    private static TenantSlug NewSlug() =>
        TenantSlug.Create("t-" + Guid.NewGuid().ToString("N")[..12]);
}

