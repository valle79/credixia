using Lending.Domain.Common;

namespace Lending.Domain.Platform;

/// <summary>
/// Global platform configuration. There is a single row; it is managed exclusively by
/// platform administrators and never scoped to a tenant.
/// </summary>
public sealed class PlatformSettings : Entity
{
    public const int MinPasswordLengthFloor = 10;
    public const int MinPasswordLengthCeiling = 128;
    public const int MaxFailedAttemptsFloor = 1;
    public const int MaxFailedAttemptsCeiling = 20;
    public const int LockoutMinutesFloor = 1;
    public const int LockoutMinutesCeiling = 1440;
    public const int RefreshTokenDaysFloor = 1;
    public const int RefreshTokenDaysCeiling = 90;
    public const int EmailHostMaxLength = 255;
    public const int EmailUsernameMaxLength = 320;
    public const int EmailAddressMaxLength = 320;
    public const int EmailDisplayNameMaxLength = 120;
    public const int EmailErrorMaxLength = 500;
    public const int EmailPasswordCipherMaxLength = 2000;

    private PlatformSettings()
    {
    }

    private PlatformSettings(DateTimeOffset now)
    {
        MinPasswordLength = 12;
        RequireLetterAndDigit = true;
        MaxFailedAttempts = 5;
        LockoutMinutes = 15;
        RefreshTokenDays = 14;
        EmailPort = 587;
        EmailUseSsl = true;
        NotifyTenantCreated = true;
        NotifyTenantSuspended = true;
        NotifyTenantReactivated = true;
        NotifySuspiciousAccess = true;
        NotifyEmailFailures = true;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public int MinPasswordLength { get; private set; }

    public bool RequireLetterAndDigit { get; private set; }

    public int MaxFailedAttempts { get; private set; }

    public int LockoutMinutes { get; private set; }

    public int RefreshTokenDays { get; private set; }

    public bool EmailEnabled { get; private set; }

    public string? EmailHost { get; private set; }

    public int EmailPort { get; private set; }

    public bool EmailUseSsl { get; private set; }

    public string? EmailUsername { get; private set; }

    public string? EmailPasswordCiphertext { get; private set; }

    public string? EmailFromAddress { get; private set; }

    public string? EmailFromName { get; private set; }

    public DateTimeOffset? EmailLastTestAt { get; private set; }

    public bool? EmailLastTestSucceeded { get; private set; }

    public string? EmailLastTestError { get; private set; }

    public bool NotifyTenantCreated { get; private set; }

    public bool NotifyTenantSuspended { get; private set; }

    public bool NotifyTenantReactivated { get; private set; }

    public bool NotifySuspiciousAccess { get; private set; }

    public bool NotifyEmailFailures { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool HasEmailPassword =>
        !string.IsNullOrWhiteSpace(EmailPasswordCiphertext);

    public static PlatformSettings Create(DateTimeOffset now)
    {
        EnsureTimestamp(now);
        return new PlatformSettings(now);
    }

    public void UpdateSecurity(
        int minPasswordLength,
        bool requireLetterAndDigit,
        int maxFailedAttempts,
        int lockoutMinutes,
        int refreshTokenDays,
        DateTimeOffset now)
    {
        EnsureTimestamp(now);

        if (minPasswordLength is < MinPasswordLengthFloor or > MinPasswordLengthCeiling)
        {
            throw new DomainException(
                "platform.settings.password_length_invalid",
                $"La longitud mínima debe estar entre {MinPasswordLengthFloor} y {MinPasswordLengthCeiling}.");
        }

        if (maxFailedAttempts is < MaxFailedAttemptsFloor or > MaxFailedAttemptsCeiling)
        {
            throw new DomainException(
                "platform.settings.failed_attempts_invalid",
                $"Los intentos máximos deben estar entre {MaxFailedAttemptsFloor} y {MaxFailedAttemptsCeiling}.");
        }

        if (lockoutMinutes is < LockoutMinutesFloor or > LockoutMinutesCeiling)
        {
            throw new DomainException(
                "platform.settings.lockout_invalid",
                $"El bloqueo debe estar entre {LockoutMinutesFloor} y {LockoutMinutesCeiling} minutos.");
        }

        if (refreshTokenDays is < RefreshTokenDaysFloor or > RefreshTokenDaysCeiling)
        {
            throw new DomainException(
                "platform.settings.session_days_invalid",
                $"La duración de sesión debe estar entre {RefreshTokenDaysFloor} y {RefreshTokenDaysCeiling} días.");
        }

        MinPasswordLength = minPasswordLength;
        RequireLetterAndDigit = requireLetterAndDigit;
        MaxFailedAttempts = maxFailedAttempts;
        LockoutMinutes = lockoutMinutes;
        RefreshTokenDays = refreshTokenDays;
        UpdatedAt = now;
    }

    public void UpdateEmail(
        bool enabled,
        string? host,
        int port,
        bool useSsl,
        string? username,
        string? fromAddress,
        string? fromName,
        DateTimeOffset now)
    {
        EnsureTimestamp(now);

        if (port is < 1 or > 65535)
        {
            throw new DomainException(
                "platform.settings.email_port_invalid",
                "El puerto SMTP debe estar entre 1 y 65535.");
        }

        var normalizedHost = NormalizeOptional(host, EmailHostMaxLength, "host SMTP");
        var normalizedUsername = NormalizeOptional(username, EmailUsernameMaxLength, "usuario SMTP");
        var normalizedFrom = NormalizeOptional(fromAddress, EmailAddressMaxLength, "remitente");
        var normalizedName = NormalizeOptional(fromName, EmailDisplayNameMaxLength, "nombre del remitente");

        if (enabled && (normalizedHost is null || normalizedFrom is null))
        {
            throw new DomainException(
                "platform.settings.email_incomplete",
                "Para habilitar el envío debes indicar host y remitente.");
        }

        EmailEnabled = enabled;
        EmailHost = normalizedHost;
        EmailPort = port;
        EmailUseSsl = useSsl;
        EmailUsername = normalizedUsername;
        EmailFromAddress = normalizedFrom;
        EmailFromName = normalizedName;

        if (!enabled)
        {
            EmailLastTestSucceeded = null;
            EmailLastTestError = null;
        }

        UpdatedAt = now;
    }

    public void SetEmailPassword(string? ciphertext, DateTimeOffset now)
    {
        EnsureTimestamp(now);

        if (ciphertext is { Length: > EmailPasswordCipherMaxLength })
        {
            throw new DomainException(
                "platform.settings.email_password_too_long",
                "La contraseña cifrada excede el tamaño permitido.");
        }

        EmailPasswordCiphertext = string.IsNullOrWhiteSpace(ciphertext) ? null : ciphertext;
        UpdatedAt = now;
    }

    public void UpdateNotificationPreferences(
        bool tenantCreated,
        bool tenantSuspended,
        bool tenantReactivated,
        bool suspiciousAccess,
        bool emailFailures,
        DateTimeOffset now)
    {
        EnsureTimestamp(now);

        NotifyTenantCreated = tenantCreated;
        NotifyTenantSuspended = tenantSuspended;
        NotifyTenantReactivated = tenantReactivated;
        NotifySuspiciousAccess = suspiciousAccess;
        NotifyEmailFailures = emailFailures;
        UpdatedAt = now;
    }

    public void RecordEmailTest(DateTimeOffset now, bool succeeded, string? error)
    {
        EnsureTimestamp(now);

        EmailLastTestAt = now;
        EmailLastTestSucceeded = succeeded;
        EmailLastTestError = succeeded ? null : NormalizeError(error);
        UpdatedAt = now;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(
                "platform.settings.value_too_long",
                $"El {label} no puede exceder {maxLength} caracteres.");
        }

        return trimmed;
    }

    private static string? NormalizeError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return null;
        }

        var trimmed = error.Trim();
        return trimmed.Length > EmailErrorMaxLength
            ? trimmed[..EmailErrorMaxLength]
            : trimmed;
    }

    private static void EnsureTimestamp(DateTimeOffset now)
    {
        if (now == default)
        {
            throw new ArgumentException("A valid timestamp is required.", nameof(now));
        }
    }
}
