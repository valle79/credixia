using System.Security.Claims;
using Lending.Api.Authentication;
using Lending.Api.Tenancy;
using Lending.Domain.Identity;
using Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lending.Api.Middleware;

/// <summary>
/// Resolves the authenticated user and, when the access token carries a tenant claim,
/// validates the live membership before the endpoint runs. The database transaction makes
/// the RLS session variables valid for the whole request and rolls back on failure.
/// </summary>
public sealed class SessionContextMiddleware
{
    private readonly RequestDelegate _next;

    public SessionContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        LendingDbContext dbContext,
        RequestTenantContext tenantContext,
        CurrentUser currentUser,
        ILogger<SessionContextMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(tenantContext);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(logger);

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userIdValue = context.User.FindFirstValue("sub");
        var securityStamp = context.User.FindFirstValue("sst");

        if (!Guid.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(securityStamp))
        {
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Sesión inválida.");
            return;
        }

        var tenantIdValue = context.User.FindFirstValue("tid");
        Guid? requestedTenantId = null;
        if (!string.IsNullOrWhiteSpace(tenantIdValue))
        {
            if (!Guid.TryParse(tenantIdValue, out var parsedTenantId))
            {
                await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Sesión inválida.");
                return;
            }

            requestedTenantId = parsedTenantId;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, context.RequestAborted);

        if (user is null || !user.IsActive)
        {
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Sesión inválida.");
            return;
        }

        if (!string.Equals(user.SecurityStamp, securityStamp, StringComparison.Ordinal))
        {
            logger.LogWarning("Rejected access token with a stale security stamp for user {UserId}.", userId);
            await WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "La sesión expiró. Inicia sesión nuevamente.");
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            context.RequestAborted);

        await SetSessionVariablesAsync(
            dbContext,
            user.Id,
            user.IsPlatformAdmin,
            requestedTenantId,
            context.RequestAborted);

        Guid? tenantId = null;
        string? tenantName = null;
        string? tenantSlug = null;
        string? tenantLogoUrl = null;
        var permissions = Array.Empty<string>();

        if (requestedTenantId is { } tenantToLoad)
        {
            var tenant = await dbContext.Tenants
                .AsNoTracking()
                .SingleOrDefaultAsync(candidate => candidate.Id == tenantToLoad, context.RequestAborted);

            if (tenant is null || !tenant.CanOperate)
            {
                await WriteProblemAsync(
                    context,
                    StatusCodes.Status403Forbidden,
                    "La organización no está disponible.");
                return;
            }

            var membership = await SessionQueries.LoadMembershipAsync(
                dbContext,
                tenantToLoad,
                userId,
                context.RequestAborted);

            if (membership is null || !membership.IsActive)
            {
                await WriteProblemAsync(
                    context,
                    StatusCodes.Status403Forbidden,
                    "No tienes acceso a esta organización.");
                return;
            }

            tenantId = tenantToLoad;
            tenantName = tenant.Name;
            tenantSlug = tenant.Slug.Value;
            tenantLogoUrl = tenant.LogoUrl;
            tenantContext.Set(tenantToLoad);

            permissions = SessionQueries.ResolvePermissions(membership).ToArray();
        }

        if (user.IsPlatformAdmin)
        {
            permissions = permissions
                .Append(Permissions.PlatformTenantsManage)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        currentUser.Set(
            user.Id,
            user.Email.Value,
            user.FullName,
            user.IsPlatformAdmin,
            tenantId,
            tenantName,
            tenantSlug,
            tenantLogoUrl,
            permissions);

        await _next(context);

        await transaction.CommitAsync(context.RequestAborted);
    }

    private static Task<int> SetSessionVariablesAsync(
        LendingDbContext dbContext,
        Guid userId,
        bool isPlatformAdmin,
        Guid? tenantId,
        CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.user_id', {userId.ToString()}, true), set_config('app.is_platform_admin', {(isPlatformAdmin ? "true" : "false")}, true), set_config('app.tenant_id', {tenantId?.ToString() ?? string.Empty}, true)",
            cancellationToken);

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        await Results.Problem(statusCode: statusCode, detail: detail).ExecuteAsync(context);
    }
}
