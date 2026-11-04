using Lending.Domain.Common;
using Lending.Domain.Tenancy;

namespace Lending.UnitTests.Tenancy;

public sealed class TenantTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_starts_active_and_trims_name()
    {
        var tenant = Tenant.Create(TenantSlug.Create("acme"), "  Acme SAC  ", _now);

        Assert.Equal("Acme SAC", tenant.Name);
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.True(tenant.CanOperate);
        Assert.Equal(_now, tenant.CreatedAt);
        Assert.Equal(_now, tenant.UpdatedAt);
        Assert.NotEqual(Guid.Empty, tenant.Id);
    }

    [Fact]
    public void Create_generates_uuid_version_7()
    {
        var tenant = Tenant.Create(TenantSlug.Create("acme"), "Acme", _now);

        Assert.Equal(7, tenant.Id.Version);
    }

    [Fact]
    public void Suspend_then_reactivate_follows_lifecycle()
    {
        var tenant = Tenant.Create(TenantSlug.Create("acme"), "Acme", _now);
        var suspendedAt = _now.AddMinutes(5);

        tenant.Suspend(suspendedAt);

        Assert.Equal(TenantStatus.Suspended, tenant.Status);
        Assert.False(tenant.CanOperate);
        Assert.Equal(suspendedAt, tenant.UpdatedAt);

        tenant.Reactivate(suspendedAt.AddMinutes(1));

        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.True(tenant.CanOperate);
    }

    [Fact]
    public void Suspend_twice_throws_invalid_transition()
    {
        var tenant = Tenant.Create(TenantSlug.Create("acme"), "Acme", _now);
        tenant.Suspend(_now);

        var exception = Assert.Throws<DomainException>(() => tenant.Suspend(_now));

        Assert.Equal("tenant.status.invalid_transition", exception.Code);
    }

    [Fact]
    public void Reactivate_active_tenant_throws_invalid_transition()
    {
        var tenant = Tenant.Create(TenantSlug.Create("acme"), "Acme", _now);

        var exception = Assert.Throws<DomainException>(() => tenant.Reactivate(_now));

        Assert.Equal("tenant.status.invalid_transition", exception.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_without_name_throws(string? name)
    {
        var exception = Assert.Throws<DomainException>(
            () => Tenant.Create(TenantSlug.Create("acme"), name!, _now));

        Assert.Equal("tenant.name.required", exception.Code);
    }

    [Fact]
    public void Create_with_too_long_name_throws()
    {
        var name = new string('a', Tenant.NameMaxLength + 1);

        var exception = Assert.Throws<DomainException>(
            () => Tenant.Create(TenantSlug.Create("acme"), name, _now));

        Assert.Equal("tenant.name.too_long", exception.Code);
    }

    [Fact]
    public void Create_with_default_timestamp_throws()
    {
        Assert.Throws<ArgumentException>(
            () => Tenant.Create(TenantSlug.Create("acme"), "Acme", default));
    }

    [Fact]
    public void Rename_trims_and_updates_timestamp()
    {
        var tenant = Tenant.Create(TenantSlug.Create("acme"), "Acme", _now);
        var later = _now.AddHours(1);

        tenant.Rename("  Acme Capital  ", later);

        Assert.Equal("Acme Capital", tenant.Name);
        Assert.Equal(later, tenant.UpdatedAt);
    }
}

