using Lending.Application.Abstractions;

namespace Lending.Api.Tenancy;

/// <summary>
/// Request-scoped tenant context. Phase 2 will populate it exclusively from a validated
/// session plus an active tenant membership. Nothing in this class reads request headers.
/// </summary>
public sealed class RequestTenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public bool IsResolved => TenantId.HasValue;

    public Guid RequireTenantId() =>
        TenantId ?? throw new InvalidOperationException("Tenant context has not been resolved.");

    public void Set(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        if (TenantId.HasValue && TenantId.Value != tenantId)
        {
            throw new InvalidOperationException("Tenant context is already resolved for another tenant.");
        }

        TenantId = tenantId;
    }
}
