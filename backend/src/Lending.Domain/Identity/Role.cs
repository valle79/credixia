using Lending.Domain.Common;

namespace Lending.Domain.Identity;

/// <summary>
/// Tenant-scoped role. System roles are seeded when the tenant is created and cannot be
/// deleted, but their permission set can be extended by the owner in a later iteration.
/// </summary>
public sealed class Role : Entity
{
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 100;

    private readonly List<RolePermission> _permissions = [];

    private Role()
    {
    }

    private Role(Guid tenantId, string code, string name, bool isSystem, DateTimeOffset now)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        IsSystem = isSystem;
        CreatedAt = now;
    }

    public Guid TenantId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsSystem { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions;

    public static Role CreateForTenant(
        Guid tenantId,
        RoleDefinition definition,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        var role = new Role(tenantId, definition.Code, definition.Name, isSystem: true, now);
        foreach (var permission in definition.Permissions.Distinct(StringComparer.Ordinal))
        {
            role._permissions.Add(new RolePermission(role.Id, permission));
        }

        return role;
    }

    public bool Grants(string permissionCode) =>
        _permissions.Any(permission =>
            string.Equals(permission.PermissionCode, permissionCode, StringComparison.Ordinal));
}
