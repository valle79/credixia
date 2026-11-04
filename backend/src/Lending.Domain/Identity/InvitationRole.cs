namespace Lending.Domain.Identity;

public sealed class InvitationRole
{
    private InvitationRole()
    {
    }

    internal InvitationRole(Guid invitationId, string roleCode)
    {
        InvitationId = invitationId;
        RoleCode = roleCode;
    }

    public Guid InvitationId { get; private set; }

    public string RoleCode { get; private set; } = string.Empty;
}
