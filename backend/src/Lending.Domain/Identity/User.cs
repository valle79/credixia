using Lending.Domain.Common;

namespace Lending.Domain.Identity;

/// <summary>
/// Platform-level account. A user has no implicit tenant access: every tenant operation
/// requires an active membership.
/// </summary>
public sealed class User : Entity
{
    public const int NameMaxLength = 200;

    private User()
    {
    }

    private User(
        Email email,
        string fullName,
        string passwordHash,
        bool isPlatformAdmin,
        DateTimeOffset now)
    {
        Email = email;
        FullName = NormalizeName(fullName);
        PasswordHash = RequirePasswordHash(passwordHash);
        IsPlatformAdmin = isPlatformAdmin;
        Status = UserStatus.Active;
        SecurityStamp = NewSecurityStamp();
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Email Email { get; private set; } = null!;

    public string FullName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserStatus Status { get; private set; }

    public bool IsPlatformAdmin { get; private set; }

    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    public string SecurityStamp { get; private set; } = string.Empty;

    public int FailedAccessCount { get; private set; }

    public DateTimeOffset? LockoutUntil { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsActive => Status == UserStatus.Active;

    public bool IsLockedOut(DateTimeOffset now) => LockoutUntil.HasValue && LockoutUntil > now;

    public static User Create(
        Email email,
        string fullName,
        string passwordHash,
        DateTimeOffset now,
        bool isPlatformAdmin = false)
    {
        ArgumentNullException.ThrowIfNull(email);
        EnsureTimestamp(now);
        return new User(email, fullName, passwordHash, isPlatformAdmin, now);
    }

    public void RecordSuccessfulLogin(DateTimeOffset now)
    {
        EnsureTimestamp(now);
        FailedAccessCount = 0;
        LockoutUntil = null;
        LastLoginAt = now;
        UpdatedAt = now;
    }

    public void RecordFailedLogin(
        DateTimeOffset now,
        int maxFailedAttempts,
        TimeSpan lockoutDuration)
    {
        EnsureTimestamp(now);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxFailedAttempts, 1);

        FailedAccessCount++;
        if (FailedAccessCount >= maxFailedAttempts)
        {
            LockoutUntil = now.Add(lockoutDuration);
        }

        UpdatedAt = now;
    }

    public void SetPassword(string passwordHash, DateTimeOffset now)
    {
        EnsureTimestamp(now);
        PasswordHash = RequirePasswordHash(passwordHash);
        SecurityStamp = NewSecurityStamp();
        FailedAccessCount = 0;
        LockoutUntil = null;
        UpdatedAt = now;
    }

    public void VerifyEmail(DateTimeOffset now)
    {
        EnsureTimestamp(now);
        EmailVerifiedAt ??= now;
        UpdatedAt = now;
    }

    public void Rename(string fullName, DateTimeOffset now)
    {
        EnsureTimestamp(now);
        FullName = NormalizeName(fullName);
        UpdatedAt = now;
    }

    public void Suspend(DateTimeOffset now)
    {
        EnsureTimestamp(now);
        if (Status == UserStatus.Suspended)
        {
            return;
        }

        Status = UserStatus.Suspended;
        SecurityStamp = NewSecurityStamp();
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        EnsureTimestamp(now);
        if (Status == UserStatus.Active)
        {
            return;
        }

        Status = UserStatus.Active;
        UpdatedAt = now;
    }

    private static string NormalizeName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("user.name.required", "User full name is required.");
        }

        var trimmed = fullName.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException(
                "user.name.too_long",
                $"User full name cannot exceed {NameMaxLength} characters.");
        }

        return trimmed;
    }

    private static string RequirePasswordHash(string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("user.password_hash.required", "Password hash is required.");
        }

        return passwordHash;
    }

    private static string NewSecurityStamp() => Guid.NewGuid().ToString("N");

    private static void EnsureTimestamp(DateTimeOffset now)
    {
        if (now == default)
        {
            throw new ArgumentException("A valid timestamp is required.", nameof(now));
        }
    }
}
