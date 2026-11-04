using Lending.Domain.Common;

namespace Lending.Domain.Identity;

public sealed class PasswordResetToken : Entity
{
    private PasswordResetToken()
    {
    }

    private PasswordResetToken(
        Guid userId,
        string tokenHash,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = now;
        ExpiresAt = expiresAt;
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public bool IsActive(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;

    public static PasswordResetToken Create(
        Guid userId,
        string tokenHash,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresAt, now);

        return new PasswordResetToken(userId, tokenHash, now, expiresAt);
    }

    public void MarkUsed(DateTimeOffset now)
    {
        if (!IsActive(now))
        {
            throw new DomainException(
                "password_reset.not_active",
                "The password reset token is expired or already used.");
        }

        UsedAt = now;
    }
}
