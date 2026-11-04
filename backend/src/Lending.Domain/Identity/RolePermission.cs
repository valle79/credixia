namespace Lending.Domain.Identity;

/// <summary>
/// Permission granted to a role. Permissions are a stable platform catalog, not tenant data.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "The suffix describes the granted permission and matches the permission catalog.")]
public sealed class RolePermission
{
    private RolePermission()
    {
    }

    internal RolePermission(Guid roleId, string permissionCode)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role identifier cannot be empty.", nameof(roleId));
        }

        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            throw new ArgumentException("Permission code is required.", nameof(permissionCode));
        }

        RoleId = roleId;
        PermissionCode = permissionCode;
    }

    public Guid RoleId { get; private set; }

    public string PermissionCode { get; private set; } = string.Empty;
}
