using Lending.Domain.Common;

namespace Lending.Domain.Identity;

/// <summary>
/// Opaque refresh token stored only as a hash. Rotation creates a new token in the same
/// family; presenting a revoked token again revokes the whole family.
/// </summary>
public sealed class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid userId,
        Guid? tenantId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        string? createdByIpHash,
        string? userAgent)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        if (familyId == Guid.Empty)
        {
            throw new ArgumentException("Token family cannot be empty.", nameof(familyId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        }

        UserId = userId;
        TenantId = tenantId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        CreatedAt = now;
        ExpiresAt = expiresAt;
        CreatedByIpHash = createdByIpHash;
        UserAgent = userAgent;
    }

    public Guid UserId { get; private set; }

    public Guid? TenantId { get; private set; }

    public Guid FamilyId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public string? CreatedByIpHash { get; private set; }

    public string? UserAgent { get; private set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    public static RefreshToken Create(
        Guid userId,
        Guid? tenantId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        string? createdByIpHash = null,
        string? userAgent = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresAt, now);

        return new RefreshToken(
            userId,
            tenantId,
            familyId,
            tokenHash,
            now,
            expiresAt,
            createdByIpHash,
            userAgent);
    }

    public void Revoke(DateTimeOffset now, Guid? replacedByTokenId = null)
    {
        if (RevokedAt.HasValue)
        {
            return;
        }

        RevokedAt = now;
        ReplacedByTokenId = replacedByTokenId;
    }
}
