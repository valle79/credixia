using Lending.Domain.Common;
using Lending.Domain.ValueObjects;

namespace Lending.Domain.Tenancy;

/// <summary>
/// Tenant-specific operational defaults. Historical operations snapshot their own values,
/// so changing settings never rewrites the past.
/// </summary>
public sealed class TenantSettings : Entity
{
    public const int TimezoneMaxLength = 80;
    public const int LocaleMaxLength = 15;
    public const int BusinessDayRuleMaxLength = 40;

    private TenantSettings()
    {
    }

    private TenantSettings(
        Guid tenantId,
        string timezone,
        Currency currency,
        string locale,
        string businessDayRule,
        DateTimeOffset now)
    {
        TenantId = tenantId;
        Timezone = timezone;
        Currency = currency;
        Locale = locale;
        BusinessDayRule = businessDayRule;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid TenantId { get; private set; }

    public string Timezone { get; private set; } = "America/Lima";

    public Currency Currency { get; private set; } = Currency.Pen;

    public string Locale { get; private set; } = "es-PE";

    public string BusinessDayRule { get; private set; } = "NextBusinessDay";

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static TenantSettings CreateDefault(Guid tenantId, DateTimeOffset now)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        return new TenantSettings(
            tenantId,
            "America/Lima",
            Currency.Pen,
            "es-PE",
            "NextBusinessDay",
            now);
    }

    public void Update(
        string timezone,
        Currency currency,
        string locale,
        string businessDayRule,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(currency);
        Timezone = RequireBounded(timezone, "settings.timezone.invalid", TimezoneMaxLength);
        Currency = currency;
        Locale = RequireBounded(locale, "settings.locale.invalid", LocaleMaxLength);
        BusinessDayRule = RequireBounded(
            businessDayRule,
            "settings.business_day_rule.invalid",
            BusinessDayRuleMaxLength);
        UpdatedAt = now;
    }

    private static string RequireBounded(string? value, string code, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(code, "A value is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(code, $"Value cannot exceed {maxLength} characters.");
        }

        return trimmed;
    }
}
