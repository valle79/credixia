using Lending.Domain.Identity;
using Lending.Domain.Tenancy;
using Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lending.Api.Authentication;

public static class SessionQueries
{
    public static Task<TenantMembership?> LoadMembershipAsync(
        LendingDbContext dbContext,
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.TenantMemberships
            .AsNoTracking()
            .Include(membership => membership.Roles)
            .ThenInclude(role => role.Role)
            .ThenInclude(role => role.Permissions)
            .AsSingleQuery()
            .SingleOrDefaultAsync(
                membership => membership.TenantId == tenantId && membership.UserId == userId,
                cancellationToken);

    public static Task<Tenant?> LoadTenantAsync(
        LendingDbContext dbContext,
        Guid tenantId,
        CancellationToken cancellationToken) =>
        dbContext.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(tenant => tenant.Id == tenantId, cancellationToken);

    public static IReadOnlyList<string> ResolvePermissions(TenantMembership membership) =>
        membership.Roles
            .SelectMany(role => role.Role.Permissions)
            .Select(permission => permission.PermissionCode)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
}
