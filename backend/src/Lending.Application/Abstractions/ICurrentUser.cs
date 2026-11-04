namespace Lending.Application.Abstractions;

/// <summary>
/// Authenticated principal for the current request. Permissions are resolved from the
/// database on each request, never trusted from token claims.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }

    bool IsPlatformAdmin { get; }

    Guid? TenantId { get; }

    IReadOnlySet<string> Permissions { get; }

    bool HasPermission(string permissionCode);
}
