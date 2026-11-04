using Lending.Domain.Identity;
using Lending.Domain.Tenancy;
using Lending.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lending.IntegrationTests.Persistence;

[Collection(PostgresFixtureDefinition.Name)]
public sealed class RowLevelSecurityTests
{
    private const string ApplicationRole = "lending_rls";
    private readonly PostgresFixture _fixture;

    public RowLevelSecurityTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Tenant_session_only_sees_its_own_rows()
    {
        var data = await SeedTwoTenantsAsync();

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"SET ROLE {ApplicationRole}");
        await SetConfigAsync(connection, "app.tenant_id", data.TenantAId.ToString());
        await SetConfigAsync(connection, "app.user_id", data.UserAId.ToString());
        await SetConfigAsync(connection, "app.is_platform_admin", "false");

        Assert.Equal(1L, await ScalarAsync(connection, "SELECT count(*) FROM tenant_settings"));
        Assert.Equal(1L, await ScalarAsync(
            connection,
            "SELECT count(*) FROM tenant_settings WHERE tenant_id = @tenant",
            ("tenant", data.TenantAId)));

        Assert.Equal(
            (long)SystemRoles.All.Count,
            await ScalarAsync(
                connection,
                "SELECT count(*) FROM roles WHERE tenant_id = @tenant",
                ("tenant", data.TenantAId)));

        Assert.Equal(0L, await ScalarAsync(
            connection,
            "SELECT count(*) FROM roles WHERE tenant_id <> @tenant",
            ("tenant", data.TenantAId)));

        Assert.Equal(0L, await ScalarAsync(
            connection,
            "SELECT count(*) FROM role_permissions rp JOIN roles r ON r.id = rp.role_id WHERE r.tenant_id <> @tenant",
            ("tenant", data.TenantAId)));

        Assert.Equal(0L, await ScalarAsync(
            connection,
            "SELECT count(*) FROM tenant_memberships WHERE tenant_id <> @tenant",
            ("tenant", data.TenantAId)));
    }

    [Fact]
    public async Task User_can_list_own_memberships_before_selecting_a_tenant()
    {
        var data = await SeedTwoTenantsAsync();

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"SET ROLE {ApplicationRole}");
        await SetConfigAsync(connection, "app.tenant_id", string.Empty);
        await SetConfigAsync(connection, "app.user_id", data.UserAId.ToString());
        await SetConfigAsync(connection, "app.is_platform_admin", "false");

        Assert.Equal(1L, await ScalarAsync(connection, "SELECT count(*) FROM tenant_memberships"));
        Assert.Equal(1L, await ScalarAsync(
            connection,
            "SELECT count(*) FROM tenant_memberships WHERE user_id = @user",
            ("user", data.UserAId)));
        Assert.Equal(0L, await ScalarAsync(
            connection,
            "SELECT count(*) FROM tenant_memberships WHERE user_id = @user",
            ("user", data.UserBId)));
    }

    [Fact]
    public async Task Platform_admin_session_can_read_across_tenants()
    {
        var data = await SeedTwoTenantsAsync();

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"SET ROLE {ApplicationRole}");
        await SetConfigAsync(connection, "app.is_platform_admin", "true");
        await SetConfigAsync(connection, "app.tenant_id", string.Empty);

        Assert.Equal(2L, await ScalarAsync(
            connection,
            "SELECT count(*) FROM tenant_settings WHERE tenant_id IN (@tenantA, @tenantB)",
            ("tenantA", data.TenantAId),
            ("tenantB", data.TenantBId)));
    }

    private async Task<SeededData> SeedTwoTenantsAsync()
    {
        await using var context = _fixture.CreateContext();
        var now = DateTimeOffset.UtcNow;

        var userA = User.Create(
            Email.Create($"rls-a-{Guid.NewGuid():N}@example.test"),
            "User A",
            "unused-hash",
            now);
        var userB = User.Create(
            Email.Create($"rls-b-{Guid.NewGuid():N}@example.test"),
            "User B",
            "unused-hash",
            now);
        context.Users.AddRange(userA, userB);

        var tenantA = Tenant.Create(TenantSlug.Create("rls-a-" + Guid.NewGuid().ToString("N")[..8]), "Tenant A", now);
        var tenantB = Tenant.Create(TenantSlug.Create("rls-b-" + Guid.NewGuid().ToString("N")[..8]), "Tenant B", now);
        context.Tenants.AddRange(tenantA, tenantB);
        context.TenantSettings.AddRange(
            TenantSettings.CreateDefault(tenantA.Id, now),
            TenantSettings.CreateDefault(tenantB.Id, now));

        var rolesA = SystemRoles.All
            .Select(definition => Role.CreateForTenant(tenantA.Id, definition, now))
            .ToList();
        var rolesB = SystemRoles.All
            .Select(definition => Role.CreateForTenant(tenantB.Id, definition, now))
            .ToList();
        context.Roles.AddRange(rolesA);
        context.Roles.AddRange(rolesB);

        context.TenantMemberships.Add(TenantMembership.CreateActive(
            tenantA.Id,
            userA.Id,
            [rolesA.First(role => role.Code == RoleCodes.Owner).Id],
            now));
        context.TenantMemberships.Add(TenantMembership.CreateActive(
            tenantB.Id,
            userB.Id,
            [rolesB.First(role => role.Code == RoleCodes.Owner).Id],
            now));

        await context.SaveChangesAsync();

        return new SeededData(
            tenantA.Id,
            tenantB.Id,
            userA.Id,
            userB.Id);
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task SetConfigAsync(
        NpgsqlConnection connection,
        string variable,
        string value)
    {
        await using var command = new NpgsqlCommand(
            "SELECT set_config(@variable, @value, false)",
            connection);
        command.Parameters.AddWithValue("variable", variable);
        command.Parameters.AddWithValue("value", value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> ScalarAsync(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed record SeededData(
        Guid TenantAId,
        Guid TenantBId,
        Guid UserAId,
        Guid UserBId);
}
