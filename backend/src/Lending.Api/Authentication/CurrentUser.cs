using Lending.Application.Abstractions;

namespace Lending.Api.Authentication;

public sealed class CurrentUser : ICurrentUser
{
    private readonly HashSet<string> _permissions = new(StringComparer.Ordinal);

    public Guid? UserId { get; private set; }

    public string? Email { get; private set; }

    public string? FullName { get; private set; }

    public bool IsAuthenticated => UserId.HasValue;

    public bool IsPlatformAdmin { get; private set; }

    public Guid? TenantId { get; private set; }

    public string? TenantName { get; private set; }

    public string? TenantSlug { get; private set; }

    public string? TenantLogoUrl { get; private set; }

    public IReadOnlySet<string> Permissions => _permissions;

    public bool HasPermission(string permissionCode) =>
        _permissions.Contains(permissionCode);

    internal void Set(
        Guid userId,
        string email,
        string fullName,
        bool isPlatformAdmin,
        Guid? tenantId,
        string? tenantName,
        string? tenantSlug,
        string? tenantLogoUrl,
        IEnumerable<string> permissions)
    {
        UserId = userId;
        Email = email;
        FullName = fullName;
        IsPlatformAdmin = isPlatformAdmin;
        TenantId = tenantId;
        TenantName = tenantName;
        TenantSlug = tenantSlug;
        TenantLogoUrl = tenantLogoUrl;
        _permissions.Clear();

        foreach (var permission in permissions)
        {
            _permissions.Add(permission);
        }
    }
}
