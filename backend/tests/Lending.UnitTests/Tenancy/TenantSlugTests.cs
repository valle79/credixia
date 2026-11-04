using Lending.Domain.Common;
using Lending.Domain.Tenancy;

namespace Lending.UnitTests.Tenancy;

public sealed class TenantSlugTests
{
    [Theory]
    [InlineData("acme")]
    [InlineData("acme-credit")]
    [InlineData("a1b")]
    [InlineData("  Acme-Credit  ")]
    public void Create_accepts_valid_values(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var slug = TenantSlug.Create(value);

        Assert.Equal(value.Trim().ToLowerInvariant(), slug.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("acme_credit")]
    [InlineData("-acme")]
    [InlineData("acme-")]
    [InlineData("acme--credit")]
    [InlineData("acme credit")]
    public void Create_rejects_invalid_values(string? value)
    {
        var exception = Assert.Throws<DomainException>(() => TenantSlug.Create(value));

        Assert.Equal("tenant.slug.invalid", exception.Code);
    }

    [Fact]
    public void Create_accepts_minimum_length()
    {
        Assert.Equal("abc", TenantSlug.Create("abc").Value);
    }

    [Fact]
    public void Create_accepts_maximum_length()
    {
        var value = new string('a', TenantSlug.MaxLength);

        Assert.Equal(value, TenantSlug.Create(value).Value);
    }

    [Fact]
    public void Create_rejects_value_over_maximum_length()
    {
        var value = new string('a', TenantSlug.MaxLength + 1);

        Assert.Throws<DomainException>(() => TenantSlug.Create(value));
    }
}
