using Lending.Domain.Common;

namespace Lending.Domain.Tenancy;

/// <summary>
/// Root of a lender organization. A tenant owns all business data of that lender.
/// </summary>
public sealed class Tenant : Entity
{
    public const int NameMaxLength = 200;

    private Tenant()
    {
    }

    private Tenant(TenantSlug slug, string name, DateTimeOffset now)
    {
        Slug = slug;
        Name = NormalizeName(name);
        Status = TenantStatus.Active;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public TenantSlug Slug { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public string? LogoUrl { get; private set; }

    public TenantStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool CanOperate => Status == TenantStatus.Active;

    public static Tenant Create(TenantSlug slug, string name, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(slug);
        EnsureTimestamp(now);
        return new Tenant(slug, name, now);
    }

    public void Rename(string name, DateTimeOffset now)
    {
        EnsureTimestamp(now);
        Name = NormalizeName(name);
        UpdatedAt = now;
    }

    public void SetLogo(string? logoUrl, DateTimeOffset now)
    {
        EnsureTimestamp(now);
        LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
        UpdatedAt = now;
    }

    public void Suspend(DateTimeOffset now)
    {
        EnsureTimestamp(now);
        if (Status != TenantStatus.Active)
        {
            throw new DomainException(
                "tenant.status.invalid_transition",
                $"Only an active tenant can be suspended. Current status: {Status}.");
        }

        Status = TenantStatus.Suspended;
        UpdatedAt = now;
    }

    public void Reactivate(DateTimeOffset now)
    {
        EnsureTimestamp(now);
        if (Status != TenantStatus.Suspended)
        {
            throw new DomainException(
                "tenant.status.invalid_transition",
                $"Only a suspended tenant can be reactivated. Current status: {Status}.");
        }

        Status = TenantStatus.Active;
        UpdatedAt = now;
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("tenant.name.required", "Tenant name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException(
                "tenant.name.too_long",
                $"Tenant name cannot exceed {NameMaxLength} characters.");
        }

        return trimmed;
    }

    private static void EnsureTimestamp(DateTimeOffset now)
    {
        if (now == default)
        {
            throw new ArgumentException("A valid timestamp is required.", nameof(now));
        }
    }
}
