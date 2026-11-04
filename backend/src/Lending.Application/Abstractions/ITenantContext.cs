namespace Lending.Application.Abstractions;

/// <summary>
/// Resolved tenant for the current authenticated request. Implementations must derive the
/// tenant from the session and a verified membership, never from client-supplied input.
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }

    bool IsResolved { get; }

    Guid RequireTenantId();
}
