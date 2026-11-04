using System.Text.RegularExpressions;
using Lending.Domain.Common;

namespace Lending.Domain.Tenancy;

/// <summary>
/// Public, URL-safe tenant identifier. Lowercase letters, digits and single hyphens.
/// </summary>
public sealed partial record TenantSlug
{
    public const int MinLength = 3;
    public const int MaxLength = 50;

    private TenantSlug(string value) => Value = value;

    public string Value { get; }

    public static TenantSlug Create(string? value)
    {
        var candidate = value?.Trim().ToLowerInvariant() ?? string.Empty;

        if (candidate.Length is < MinLength or > MaxLength || !SlugPattern().IsMatch(candidate))
        {
            throw new DomainException(
                "tenant.slug.invalid",
                $"Tenant slug must be {MinLength}-{MaxLength} characters using lowercase letters, digits and single hyphens.");
        }

        return new TenantSlug(candidate);
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SlugPattern();
}
