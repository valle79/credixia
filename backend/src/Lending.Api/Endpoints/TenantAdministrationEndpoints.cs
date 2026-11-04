using System.Text.Json;
using Lending.Api.Authentication;
using Lending.Api.Authorization;
using Lending.Application.Abstractions;
using Lending.Domain.Common;
using Lending.Domain.Identity;
using Lending.Domain.Tenancy;
using Lending.Domain.ValueObjects;
using Lending.Infrastructure.Persistence;
using Lending.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Lending.Api.Endpoints;

public static class TenantAdministrationEndpoints
{
    public static IEndpointRouteBuilder MapTenantAdministrationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var roles = endpoints.MapGroup("/api/v1/roles")
            .WithTags("Roles")
            .RequireAuthorization();

        roles.MapGet("/", GetRolesAsync)
            .WithName("ListRoles")
            .RequirePermission(Permissions.RolesView);

        var permissions = endpoints.MapGroup("/api/v1/permissions")
            .WithTags("Permissions")
            .RequireAuthorization();

        permissions.MapGet("/", GetPermissions)
            .WithName("ListPermissions")
            .RequirePermission(Permissions.RolesView);

        var tenants = endpoints.MapGroup("/api/v1/tenants")
            .WithTags("Tenants")
            .RequireAuthorization();

        tenants.MapGet("/current", GetCurrentTenantAsync)
            .WithName("GetCurrentTenant")
            .RequirePermission(Permissions.SettingsView);

        tenants.MapPut("/current", UpdateCurrentTenantAsync)
            .WithName("UpdateCurrentTenant")
            .RequirePermission(Permissions.SettingsManage);

        var audit = endpoints.MapGroup("/api/v1/audit-events")
            .WithTags("Audit")
            .RequireAuthorization();

        audit.MapGet("/", GetAuditEventsAsync)
            .WithName("ListAuditEvents")
            .RequirePermission(Permissions.AuditView);

        return endpoints;
    }

    private static async Task<IResult> GetRolesAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var tenantRoles = await dbContext.Roles
            .AsNoTracking()
            .Include(role => role.Permissions)
            .Where(role => role.TenantId == currentUser.TenantId.Value)
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(tenantRoles.Select(role => new RoleDetails(
            role.Id,
            role.Code,
            role.Name,
            role.IsSystem,
            role.Permissions
                .Select(permission => permission.PermissionCode)
                .Order(StringComparer.Ordinal)
                .ToArray())).ToArray());
    }

    private static IResult GetPermissions() =>
        TypedResults.Ok(Permissions.All.Select(permission => new PermissionDetails(
            permission.Code,
            permission.Description)).ToArray());

    private static async Task<IResult> GetCurrentTenantAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var tenant = await SessionQueries.LoadTenantAsync(
            dbContext,
            currentUser.TenantId.Value,
            cancellationToken);

        if (tenant is null)
        {
            return TenantRequired();
        }

        var settings = await dbContext.TenantSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.TenantId == tenant.Id,
                cancellationToken);

        return TypedResults.Ok(new CurrentTenantDetails(
            new TenantDetails(tenant.Id, tenant.Slug.Value, tenant.Name, tenant.Status.ToString()),
            settings is null
                ? null
                : new TenantSettingsDetails(
                    settings.Timezone,
                    settings.Currency.Code,
                    settings.Locale,
                    settings.BusinessDayRule)));
    }

    private static async Task<IResult> UpdateCurrentTenantAsync(
        UpdateTenantSettingsRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        Currency currency;
        try
        {
            currency = Currency.FromCode(request.Currency);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Moneda inválida",
                detail: exception.Message);
        }

        var settings = await dbContext.TenantSettings
            .SingleOrDefaultAsync(
                candidate => candidate.TenantId == currentUser.TenantId.Value,
                cancellationToken);

        string? before = null;
        var now = clock.UtcNow;

        if (settings is null)
        {
            settings = TenantSettings.CreateDefault(currentUser.TenantId.Value, now);
            dbContext.TenantSettings.Add(settings);
        }
        else
        {
            before = JsonSerializer.Serialize(new
            {
                settings.Timezone,
                Currency = settings.Currency.Code,
                settings.Locale,
                settings.BusinessDayRule,
            });
        }

        settings.Update(
            request.Timezone,
            currency,
            request.Locale,
            request.BusinessDayRule,
            now);

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "tenant.settings.updated",
                "TenantSettings",
                settings.Id.ToString(),
                before,
                JsonSerializer.Serialize(new
                {
                    settings.Timezone,
                    Currency = settings.Currency.Code,
                    settings.Locale,
                    settings.BusinessDayRule,
                }),
                httpContext.TraceIdentifier,
                IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        return TypedResults.Ok(new TenantSettingsDetails(
            settings.Timezone,
            settings.Currency.Code,
            settings.Locale,
            settings.BusinessDayRule));
    }

    private static async Task<IResult> GetAuditEventsAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        int? take,
        string? action,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var boundedTake = Math.Clamp(take ?? 50, 1, 200);
        var query = dbContext.AuditEvents
            .AsNoTracking()
            .Where(auditEvent => auditEvent.TenantId == currentUser.TenantId.Value);

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(auditEvent => auditEvent.Action.StartsWith(action));
        }

        var events = await query
            .OrderByDescending(auditEvent => auditEvent.OccurredAt)
            .Take(boundedTake)
            .ToListAsync(cancellationToken);

        var actorIds = events
            .Where(auditEvent => auditEvent.ActorUserId.HasValue)
            .Select(auditEvent => auditEvent.ActorUserId!.Value)
            .Distinct()
            .ToArray();

        var actors = await dbContext.Users
            .AsNoTracking()
            .Where(user => actorIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.Email.Value, cancellationToken);

        return TypedResults.Ok(events.Select(auditEvent => new AuditEventSummary(
            auditEvent.Id,
            auditEvent.Action,
            auditEvent.EntityType,
            auditEvent.EntityId,
            auditEvent.ActorUserId,
            auditEvent.ActorUserId.HasValue
                ? actors.GetValueOrDefault(auditEvent.ActorUserId.Value)
                : null,
            auditEvent.CorrelationId,
            auditEvent.OccurredAt)).ToArray());
    }

    private static ProblemHttpResult TenantRequired() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Organización requerida",
            detail: "Selecciona una organización antes de continuar.");

    public sealed record RoleDetails(
        Guid Id,
        string Code,
        string Name,
        bool IsSystem,
        IReadOnlyList<string> Permissions);

    public sealed record PermissionDetails(string Code, string Description);

    public sealed record TenantDetails(Guid Id, string Slug, string Name, string Status);

    public sealed record TenantSettingsDetails(
        string Timezone,
        string Currency,
        string Locale,
        string BusinessDayRule);

    public sealed record CurrentTenantDetails(
        TenantDetails Tenant,
        TenantSettingsDetails? Settings);

    public sealed record UpdateTenantSettingsRequest(
        string Timezone,
        string Currency,
        string Locale,
        string BusinessDayRule);

    public sealed record AuditEventSummary(
        Guid Id,
        string Action,
        string EntityType,
        string? EntityId,
        Guid? ActorUserId,
        string? ActorEmail,
        string? CorrelationId,
        DateTimeOffset OccurredAt);
}
