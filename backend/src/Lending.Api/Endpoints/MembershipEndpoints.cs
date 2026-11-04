using System.Text.Json;
using Lending.Api.Authentication;
using Lending.Api.Authorization;
using Lending.Api.Configuration;
using Lending.Application.Abstractions;
using Lending.Domain.Common;
using Lending.Domain.Identity;
using Lending.Infrastructure.Persistence;
using Lending.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lending.Api.Endpoints;

public static class MembershipEndpoints
{
    public static IEndpointRouteBuilder MapMembershipEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/memberships")
            .WithTags("Memberships")
            .RequireAuthorization();

        group.MapGet("/", GetMembershipsAsync)
            .WithName("ListMemberships")
            .RequirePermission(Permissions.MembersView);

        group.MapPatch("/{id:guid}/roles", UpdateRolesAsync)
            .WithName("UpdateMembershipRoles")
            .RequirePermission(Permissions.MembersManage);

        group.MapPatch("/{id:guid}/status", UpdateStatusAsync)
            .WithName("UpdateMembershipStatus")
            .RequirePermission(Permissions.MembersManage);

        group.MapGet("/invitations", GetInvitationsAsync)
            .WithName("ListInvitations")
            .RequirePermission(Permissions.MembersView);

        group.MapPost("/invitations", CreateInvitationAsync)
            .WithName("CreateInvitation")
            .RequirePermission(Permissions.MembersManage);

        group.MapDelete("/invitations/{id:guid}", RevokeInvitationAsync)
            .WithName("RevokeInvitation")
            .RequirePermission(Permissions.MembersManage);

        return endpoints;
    }

    private static async Task<IResult> GetMembershipsAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var memberships = await dbContext.TenantMemberships
            .AsNoTracking()
            .Include(membership => membership.Roles)
            .ThenInclude(role => role.Role)
            .Where(membership => membership.TenantId == currentUser.TenantId.Value)
            .OrderBy(membership => membership.CreatedAt)
            .ToListAsync(cancellationToken);

        var userIds = memberships.Select(membership => membership.UserId).ToArray();
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        var response = memberships
            .Select(membership =>
            {
                users.TryGetValue(membership.UserId, out var user);
                return new MembershipSummary(
                    membership.Id,
                    membership.UserId,
                    user?.Email.Value ?? string.Empty,
                    user?.FullName ?? string.Empty,
                    membership.Status.ToString(),
                    membership.Roles
                        .Select(role => new RoleSummary(role.Role.Id, role.Role.Code, role.Role.Name))
                        .OrderBy(role => role.Name, StringComparer.Ordinal)
                        .ToArray(),
                    membership.CreatedAt);
            })
            .ToArray();

        return TypedResults.Ok(response);
    }

    private static async Task<IResult> UpdateRolesAsync(
        Guid id,
        UpdateMembershipRolesRequest request,
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

        var roleCodes = request.RoleCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (roleCodes.Length == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Roles requeridos",
                detail: "Debes asignar al menos un rol.");
        }

        var roles = await dbContext.Roles
            .Where(role => role.TenantId == currentUser.TenantId.Value && roleCodes.Contains(role.Code))
            .ToListAsync(cancellationToken);

        if (roles.Count != roleCodes.Length)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Rol inválido",
                detail: "Uno de los roles seleccionados no existe en la organización.");
        }

        var membership = await dbContext.TenantMemberships
            .Include(candidate => candidate.Roles)
            .ThenInclude(role => role.Role)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id && candidate.TenantId == currentUser.TenantId.Value,
                cancellationToken);

        if (membership is null)
        {
            return TypedResults.NotFound();
        }

        var wasOwner = HasRole(membership, RoleCodes.Owner);
        var willBeOwner = roles.Any(role => role.Code == RoleCodes.Owner);

        if (wasOwner && !willBeOwner && await IsLastOwnerAsync(
                dbContext,
                currentUser.TenantId.Value,
                membership.Id,
                cancellationToken))
        {
            return LastOwnerConflict();
        }

        var before = JsonSerializer.Serialize(
            membership.Roles.Select(role => role.Role.Code).Order(StringComparer.Ordinal));

        membership.ReplaceRoles(roles.Select(role => role.Id), clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "membership.roles.updated",
                "TenantMembership",
                membership.Id.ToString(),
                before,
                JsonSerializer.Serialize(roleCodes.Order(StringComparer.Ordinal)),
                httpContext.TraceIdentifier,
                IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> UpdateStatusAsync(
        Guid id,
        UpdateMembershipStatusRequest request,
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

        var membership = await dbContext.TenantMemberships
            .Include(candidate => candidate.Roles)
            .ThenInclude(role => role.Role)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id && candidate.TenantId == currentUser.TenantId.Value,
                cancellationToken);

        if (membership is null)
        {
            return TypedResults.NotFound();
        }

        if (request.Active)
        {
            membership.Activate(clock.UtcNow);
        }
        else
        {
            if (HasRole(membership, RoleCodes.Owner)
                && await IsLastOwnerAsync(dbContext, currentUser.TenantId.Value, membership.Id, cancellationToken))
            {
                return LastOwnerConflict();
            }

            membership.Suspend(clock.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                request.Active ? "membership.activated" : "membership.suspended",
                "TenantMembership",
                membership.Id.ToString(),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetInvitationsAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var invitations = await dbContext.Invitations
            .AsNoTracking()
            .Include(invitation => invitation.Roles)
            .Where(invitation =>
                invitation.TenantId == currentUser.TenantId.Value
                && invitation.AcceptedAt == null
                && invitation.RevokedAt == null)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(invitations.Select(invitation => new InvitationSummary(
            invitation.Id,
            invitation.Email.Value,
            invitation.Roles.Select(role => role.RoleCode).Order(StringComparer.Ordinal).ToArray(),
            invitation.CreatedAt,
            invitation.ExpiresAt)).ToArray());
    }

    private static async Task<IResult> CreateInvitationAsync(
        CreateInvitationRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        ISecureTokenGenerator tokenGenerator,
        IEmailSender emailSender,
        IAuditWriter auditWriter,
        IClock clock,
        IOptions<AuthOptions> authOptions,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue || !currentUser.UserId.HasValue)
        {
            return TenantRequired();
        }

        Email email;
        try
        {
            email = Email.Create(request.Email);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Correo inválido",
                detail: exception.Message);
        }

        var roleCodes = request.RoleCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (roleCodes.Length == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Roles requeridos",
                detail: "Debes asignar al menos un rol.");
        }

        var roles = await dbContext.Roles
            .AsNoTracking()
            .Where(role => role.TenantId == currentUser.TenantId.Value && roleCodes.Contains(role.Code))
            .Select(role => role.Code)
            .ToListAsync(cancellationToken);

        if (roles.Count != roleCodes.Length)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Rol inválido",
                detail: "Uno de los roles seleccionados no existe en la organización.");
        }

        var tenant = await SessionQueries.LoadTenantAsync(
            dbContext,
            currentUser.TenantId.Value,
            cancellationToken);

        if (tenant is null)
        {
            return TenantRequired();
        }

        var existingMember = await (
                from membership in dbContext.TenantMemberships
                join user in dbContext.Users on membership.UserId equals user.Id
                where membership.TenantId == currentUser.TenantId.Value
                    && membership.Status == MembershipStatus.Active
                    && user.Email == email
                select membership.Id)
            .AnyAsync(cancellationToken);

        if (existingMember)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Usuario ya es miembro",
                detail: "Este correo ya tiene una membresía activa en la organización.");
        }

        var now = clock.UtcNow;
        var generated = tokenGenerator.Generate();
        var invitation = Invitation.Create(
            tenant.Id,
            email,
            generated.Hash,
            roleCodes,
            now,
            now.AddDays(authOptions.Value.InvitationDays),
            currentUser.UserId.Value);

        dbContext.Invitations.Add(invitation);
        await dbContext.SaveChangesAsync(cancellationToken);

        var link = $"{authOptions.Value.PublicUrl.TrimEnd('/')}/invitaciones/aceptar"
            + $"?tenant={Uri.EscapeDataString(tenant.Slug.Value)}"
            + $"&token={Uri.EscapeDataString(generated.Token)}";

        await emailSender.SendAsync(
            new EmailMessage(
                email.Value,
                $"Invitación a {tenant.Name}",
                $"Has sido invitado a {tenant.Name}. Acepta la invitación aquí: {link}. "
                + $"El enlace expira en {authOptions.Value.InvitationDays} días."),
            cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                tenant.Id,
                currentUser.UserId,
                "membership.invitation.created",
                "Invitation",
                invitation.Id.ToString(),
                AfterJson: JsonSerializer.Serialize(new { email = email.Value, roleCodes }),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        return TypedResults.Created(
            $"/api/v1/memberships/invitations/{invitation.Id}",
            new InvitationSummary(
                invitation.Id,
                invitation.Email.Value,
                roleCodes,
                invitation.CreatedAt,
                invitation.ExpiresAt));
    }

    private static async Task<IResult> RevokeInvitationAsync(
        Guid id,
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

        var invitation = await dbContext.Invitations
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id && candidate.TenantId == currentUser.TenantId.Value,
                cancellationToken);

        if (invitation is null)
        {
            return TypedResults.NotFound();
        }

        invitation.Revoke(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "membership.invitation.revoked",
                "Invitation",
                invitation.Id.ToString(),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static bool HasRole(TenantMembership membership, string roleCode) =>
        membership.Roles.Any(role =>
            string.Equals(role.Role.Code, roleCode, StringComparison.Ordinal));

    private static async Task<bool> IsLastOwnerAsync(
        LendingDbContext dbContext,
        Guid tenantId,
        Guid excludedMembershipId,
        CancellationToken cancellationToken)
    {
        var ownerCount = await dbContext.TenantMemberships
            .AsNoTracking()
            .Where(membership =>
                membership.TenantId == tenantId
                && membership.Id != excludedMembershipId
                && membership.Status == MembershipStatus.Active)
            .Where(membership => membership.Roles.Any(role => role.Role.Code == RoleCodes.Owner))
            .CountAsync(cancellationToken);

        return ownerCount == 0;
    }

    private static ProblemHttpResult TenantRequired() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Organización requerida",
            detail: "Selecciona una organización antes de continuar.");

    private static ProblemHttpResult LastOwnerConflict() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Propietario requerido",
            detail: "La organización debe conservar al menos un propietario activo.");

    public sealed record MembershipSummary(
        Guid Id,
        Guid UserId,
        string Email,
        string FullName,
        string Status,
        IReadOnlyList<RoleSummary> Roles,
        DateTimeOffset CreatedAt);

    public sealed record RoleSummary(Guid Id, string Code, string Name);

    public sealed record UpdateMembershipRolesRequest(IReadOnlyList<string> RoleCodes);

    public sealed record UpdateMembershipStatusRequest(bool Active);

    public sealed record InvitationSummary(
        Guid Id,
        string Email,
        IReadOnlyList<string> RoleCodes,
        DateTimeOffset CreatedAt,
        DateTimeOffset ExpiresAt);

    public sealed record CreateInvitationRequest(
        string Email,
        IReadOnlyList<string> RoleCodes);
}
