using Lending.Domain.Common;

namespace Lending.Domain.Platform;

/// <summary>
/// In-app notification addressed to platform administrators. It is created from real
/// events so the settings screen never shows seeded or simulated activity.
/// </summary>
public sealed class PlatformNotification : Entity
{
    public const int TitleMaxLength = 160;
    public const int MessageMaxLength = 1000;
    public const int CategoryMaxLength = 40;
    public const int SeverityMaxLength = 20;
    public const int LinkMaxLength = 300;

    public const string CategoryOrganization = "organization";
    public const string CategorySecurity = "security";
    public const string CategoryEmail = "email";
    public const string CategorySystem = "system";

    public const string SeverityInfo = "info";
    public const string SeverityWarning = "warning";
    public const string SeverityCritical = "critical";

    private PlatformNotification()
    {
    }

    private PlatformNotification(
        string title,
        string message,
        string category,
        string severity,
        string? linkUrl,
        DateTimeOffset now)
    {
        Title = title;
        Message = message;
        Category = category;
        Severity = severity;
        LinkUrl = linkUrl;
        CreatedAt = now;
    }

    public string Title { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public string Severity { get; private set; } = string.Empty;

    public string? LinkUrl { get; private set; }

    public bool IsRead { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public static PlatformNotification Create(
        string title,
        string message,
        string category,
        string severity,
        DateTimeOffset now,
        string? linkUrl = null)
    {
        EnsureTimestamp(now);

        var normalizedTitle = RequireBounded(title, TitleMaxLength, "El título");
        var normalizedMessage = RequireBounded(message, MessageMaxLength, "El mensaje");
        var normalizedCategory = RequireBounded(category, CategoryMaxLength, "La categoría");
        var normalizedSeverity = RequireBounded(severity, SeverityMaxLength, "La severidad");
        var normalizedLink = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim();

        if (normalizedLink is { Length: > LinkMaxLength })
        {
            throw new DomainException(
                "notification.link_too_long",
                $"El enlace no puede exceder {LinkMaxLength} caracteres.");
        }

        return new PlatformNotification(
            normalizedTitle,
            normalizedMessage,
            normalizedCategory,
            normalizedSeverity,
            normalizedLink,
            now);
    }

    public void MarkRead(DateTimeOffset now)
    {
        EnsureTimestamp(now);

        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAt = now;
    }

    private static string RequireBounded(string value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                "notification.value_required",
                $"{label} es obligatorio.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(
                "notification.value_too_long",
                $"{label} no puede exceder {maxLength} caracteres.");
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
