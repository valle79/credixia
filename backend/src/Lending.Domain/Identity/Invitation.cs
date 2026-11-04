using Lending.Domain.Common;

namespace Lending.Domain.Identity;

/// <summary>
/// Single-use invitation to join a tenant. Only the token hash is persisted.
/// </summary>
public sealed class Invitation : Entity
{
    private readonly List<InvitationRole> _roles = [];

    private Invitation()
    {
    }

    private Invitation(
        Guid tenantId,
        Email email,
        string tokenHash,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        Guid createdByUserId)
    {
        TenantId = tenantId;
        Email = email;
        TokenHash = tokenHash;
        CreatedAt = now;
        ExpiresAt = expiresAt;
        CreatedByUserId = createdByUserId;
    }

    public Guid TenantId { get; private set; }

    public Email Email { get; private set; } = null!;

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public IReadOnlyCollection<InvitationRole> Roles => _roles;

    public bool IsActive(DateTimeOffset now) =>
        AcceptedAt is null && RevokedAt is null && ExpiresAt > now;

    public static Invitation Create(
        Guid tenantId,
        Email email,
        string tokenHash,
        IEnumerable<string> roleCodes,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        Guid createdByUserId)
    {
        ArgumentNullException.ThrowIfNull(email);
        var roles = roleCodes.Distinct(StringComparer.Ordinal).ToArray();
        if (roles.Length == 0)
        {
            throw new DomainException("invitation.roles.required", "At least one role is required.");
        }

        var invitation = new Invitation(
            tenantId,
            email,
            tokenHash,
            now,
            expiresAt,
            createdByUserId);

        foreach (var roleCode in roles)
        {
            invitation._roles.Add(new InvitationRole(invitation.Id, roleCode));
        }

        return invitation;
    }

    public void Accept(DateTimeOffset now)
    {
        if (!IsActive(now))
        {
            throw new DomainException(
                "invitation.not_active",
                "The invitation is expired, revoked or already accepted.");
        }

        AcceptedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        if (AcceptedAt.HasValue)
        {
            throw new DomainException(
                "invitation.already_accepted",
                "An accepted invitation cannot be revoked.");
        }

        RevokedAt ??= now;
    }
}
