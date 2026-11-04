using Lending.Domain.Common;

namespace Lending.Domain.Identity;

/// <summary>
/// Link between a user and a tenant. Access is denied when this membership is not active,
/// even if the user holds a valid access token.
/// </summary>
public sealed class TenantMembership : Entity
{
    private readonly List<MembershipRole> _roles = [];
    private TenantMembership()
    {
    }

    private TenantMembership(Guid tenantId, Guid userId, DateTimeOffset now)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        TenantId = tenantId;
        UserId = userId;
        Status = MembershipStatus.Active;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public MembershipStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<MembershipRole> Roles => _roles;

    public bool IsActive => Status == MembershipStatus.Active;

    public static TenantMembership CreateActive(
        Guid tenantId,
        Guid userId,
        IEnumerable<Guid> roleIds,
        DateTimeOffset now)
    {
        var membership = new TenantMembership(tenantId, userId, now);
        membership.ReplaceRoles(roleIds, now);
        return membership;
    }

    public void ReplaceRoles(IEnumerable<Guid> roleIds, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        var distinctRoles = roleIds.Distinct().ToArray();

        if (distinctRoles.Length == 0)
        {
            throw new DomainException(
                "membership.roles.required",
                "A membership must have at least one role.");
        }

        _roles.Clear();
        foreach (var roleId in distinctRoles)
        {
            _roles.Add(new MembershipRole(Id, TenantId, roleId));
        }

        UpdatedAt = now;
    }

    public void Suspend(DateTimeOffset now)
    {
        if (Status == MembershipStatus.Suspended)
        {
            return;
        }

        Status = MembershipStatus.Suspended;
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        if (Status == MembershipStatus.Active)
        {
            return;
        }

        Status = MembershipStatus.Active;
        UpdatedAt = now;
    }
}
