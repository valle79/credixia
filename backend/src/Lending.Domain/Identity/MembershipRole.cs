namespace Lending.Domain.Identity;

public sealed class MembershipRole
{
    private MembershipRole()
    {
    }

    internal MembershipRole(Guid membershipId, Guid tenantId, Guid roleId)
    {
        MembershipId = membershipId;
        TenantId = tenantId;
        RoleId = roleId;
    }

    public Guid MembershipId { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid RoleId { get; private set; }

    public Role Role { get; private set; } = null!;
}
