using Lending.Api.Authentication;
using Lending.Api.Configuration;
using Lending.Application.Abstractions;
using Lending.Domain.Common;
using Lending.Domain.Identity;
using Lending.Domain.Tenancy;
using Lending.Infrastructure.Persistence;
using Lending.Infrastructure.Platform;
using Lending.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Lending.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .AllowAnonymous()
            .RequireRateLimiting("auth");

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshSession")
            .AllowAnonymous()
            .RequireRateLimiting("auth");

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .AllowAnonymous();

        group.MapGet("/me", GetMe)
            .WithName("GetCurrentSession")
            .RequireAuthorization();

        group.MapPost("/tenant-context", SwitchTenantAsync)
            .WithName("SwitchTenantContext")
            .RequireAuthorization();

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .WithName("ForgotPassword")
            .AllowAnonymous()
            .RequireRateLimiting("auth");

        group.MapPost("/reset-password", ResetPasswordAsync)
            .WithName("ResetPassword")
            .AllowAnonymous()
            .RequireRateLimiting("auth");

        group.MapPost("/accept-invitation", AcceptInvitationAsync)
            .WithName("AcceptInvitation")
            .AllowAnonymous()
            .RequireRateLimiting("auth");

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext httpContext,
        LendingDbContext dbContext,
        IPasswordHasher passwordHasher,
        ISecureTokenGenerator tokenGenerator,
        SessionTokenService sessionTokenService,
        IAuditWriter auditWriter,
        IClock clock,
        PlatformSettingsStore settingsStore,
        PlatformNotificationService notificationService,
        IOptions<AuthOptions> authOptions,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Email))
        {
            return InvalidCredentials();
        }

        Email email;
        try
        {
            email = Email.Create(request.Email);
        }
        catch (DomainException)
        {
            return InvalidCredentials();
        }

        var now = clock.UtcNow;
        var user = await dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null)
        {
            passwordHasher.Hash(request.Password);
            return InvalidCredentials();
        }

        if (user.IsLockedOut(now))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status423Locked,
                title: "Cuenta bloqueada",
                detail: "Demasiados intentos fallidos. Intenta de nuevo más tarde.");
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await using var failedTransaction = await BeginTransactionIfNeededAsync(
                dbContext,
                cancellationToken);
            await SetSessionVariableAsync(dbContext, "app.user_id", user.Id, cancellationToken);

            var securityPolicy = await settingsStore.GetSecurityPolicyAsync(cancellationToken);
            user.RecordFailedLogin(
                now,
                securityPolicy.MaxFailedAttempts,
                TimeSpan.FromMinutes(securityPolicy.LockoutMinutes));

            await dbContext.SaveChangesAsync(cancellationToken);
            await auditWriter.RecordAsync(
                new AuditEntry(
                    TenantId: null,
                    ActorUserId: user.Id,
                    Action: "auth.login.failed",
                    EntityType: "User",
                    EntityId: user.Id.ToString(),
                    CorrelationId: httpContext.TraceIdentifier,
                    IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
                cancellationToken);

            if (user.IsLockedOut(now))
            {
                await notificationService.NotifySuspiciousAccessAsync(
                    user.Email.Value,
                    "la cuenta fue bloqueada por demasiados intentos fallidos.",
                    cancellationToken);
            }

            if (failedTransaction is not null)
            {
                await failedTransaction.CommitAsync(cancellationToken);
            }

            return InvalidCredentials();
        }

        if (!user.IsActive)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Cuenta suspendida",
                detail: "Contacta al administrador de la plataforma.");
        }

        await using var transaction = await BeginTransactionIfNeededAsync(dbContext, cancellationToken);

        Tenant? tenant = null;
        TenantMembership? membership = null;

        if (!string.IsNullOrWhiteSpace(request.TenantSlug))
        {
            TenantSlug slug;
            try
            {
                slug = TenantSlug.Create(request.TenantSlug);
            }
            catch (DomainException)
            {
                return InvalidCredentials();
            }

            tenant = await dbContext.Tenants
                .SingleOrDefaultAsync(candidate => candidate.Slug == slug, cancellationToken);

            if (tenant is null)
            {
                return InvalidCredentials();
            }

            if (!tenant.CanOperate)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Organización no disponible",
                    detail: "La organización está suspendida.");
            }

            await SetSessionVariablesAsync(
                dbContext,
                user.Id,
                user.IsPlatformAdmin,
                tenant.Id,
                cancellationToken);
            membership = await SessionQueries.LoadMembershipAsync(
                dbContext,
                tenant.Id,
                user.Id,
                cancellationToken);
        }
        else
        {
            // Si es Platform Admin sin slug específico, ir directo al dashboard de administración
            if (user.IsPlatformAdmin)
            {
                await SetSessionVariablesAsync(
                    dbContext,
                    user.Id,
                    user.IsPlatformAdmin,
                    tenantId: null,
                    cancellationToken);

                user.RecordSuccessfulLogin(now);

                var platformSession = await sessionTokenService.CreateAsync(
                    user,
                    tenantId: null,
                    familyId: null,
                    httpContext.Connection.RemoteIpAddress?.ToString(),
                    httpContext.Request.Headers.UserAgent.ToString(),
                    cancellationToken,
                    saveChanges: false);

                await auditWriter.RecordAsync(
                    new AuditEntry(
                        TenantId: null,
                        ActorUserId: user.Id,
                        Action: "auth.login.succeeded",
                        EntityType: "User",
                        EntityId: user.Id.ToString(),
                        CorrelationId: httpContext.TraceIdentifier,
                        IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
                    cancellationToken);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                RefreshCookie.Append(
                    httpContext,
                    authOptions.Value.RefreshCookieName,
                    platformSession.RefreshToken,
                    platformSession.RefreshTokenExpiresAt);

                return TypedResults.Ok(new LoginResponse(
                    RequiresTenantSelection: false,
                    User: null,
                    Tenants: null,
                    Session: BuildSessionResponse(
                        platformSession,
                        user,
                        tenant: null,
                        [Permissions.PlatformTenantsManage])));
            }

            // Para usuarios no-admin, verificar membresías
            await SetSessionVariablesAsync(
                dbContext,
                user.Id,
                user.IsPlatformAdmin,
                tenantId: null,
                cancellationToken);

            var activeMemberships = await dbContext.TenantMemberships
                .Where(candidate => candidate.UserId == user.Id && candidate.Status == MembershipStatus.Active)
                .ToListAsync(cancellationToken);

            if (activeMemberships.Count == 0)
            {
                return NoMembership();
            }

            if (activeMemberships.Count > 1)
            {
                var tenantIds = activeMemberships.Select(candidate => candidate.TenantId).ToArray();
                var options = await dbContext.Tenants
                    .Where(candidate => tenantIds.Contains(candidate.Id) && candidate.Status == TenantStatus.Active)
                    .OrderBy(candidate => candidate.Name)
                    .Select(candidate => new TenantOption(
                        candidate.Id,
                        candidate.Slug.Value,
                        candidate.Name))
                    .ToListAsync(cancellationToken);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                return TypedResults.Ok(new LoginResponse(
                    RequiresTenantSelection: true,
                    User: new SessionUserResponse(user.Id, user.Email.Value, user.FullName, user.IsPlatformAdmin),
                    Tenants: options,
                    Session: null));
            }

            membership = activeMemberships[0];
            tenant = await SessionQueries.LoadTenantAsync(
                dbContext,
                membership.TenantId,
                cancellationToken);

            if (tenant is null || !tenant.CanOperate)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Organización no disponible",
                    detail: "La organización está suspendida.");
            }

            await SetSessionVariablesAsync(
                dbContext,
                user.Id,
                user.IsPlatformAdmin,
                tenant.Id,
                cancellationToken);
            membership = await SessionQueries.LoadMembershipAsync(
                dbContext,
                tenant.Id,
                user.Id,
                cancellationToken);
        }

        if (membership is null || !membership.IsActive)
        {
            return NoMembership();
        }

        IReadOnlyList<string> permissions = SessionQueries.ResolvePermissions(membership);
        if (user.IsPlatformAdmin)
        {
            permissions = permissions
                .Append(Permissions.PlatformTenantsManage)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        user.RecordSuccessfulLogin(now);

        var issued = await sessionTokenService.CreateAsync(
            user,
            tenant.Id,
            familyId: null,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers.UserAgent.ToString(),
            cancellationToken,
            saveChanges: false);

        await auditWriter.RecordAsync(
            new AuditEntry(
                TenantId: tenant.Id,
                ActorUserId: user.Id,
                Action: "auth.login.succeeded",
                EntityType: "User",
                EntityId: user.Id.ToString(),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        logger.LogInformation("User {UserId} signed in to tenant {TenantId}.", user.Id, tenant.Id);

        RefreshCookie.Append(
            httpContext,
            authOptions.Value.RefreshCookieName,
            issued.RefreshToken,
            issued.RefreshTokenExpiresAt);

        return TypedResults.Ok(new LoginResponse(
            RequiresTenantSelection: false,
            User: null,
            Tenants: null,
            Session: BuildSessionResponse(issued, user, tenant, permissions)));
    }

    private static async Task<IResult> RefreshAsync(
        HttpContext httpContext,
        LendingDbContext dbContext,
        ISecureTokenGenerator tokenGenerator,
        SessionTokenService sessionTokenService,
        IAuditWriter auditWriter,
        IClock clock,
        IOptions<AuthOptions> authOptions,
        CancellationToken cancellationToken)
    {
        var cookieName = authOptions.Value.RefreshCookieName;
        var rawToken = RefreshCookie.Read(httpContext, cookieName);

        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return InvalidCredentials();
        }

        var tokenHash = tokenGenerator.Hash(rawToken);
        var storedToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
        {
            RefreshCookie.Delete(httpContext, cookieName);
            return InvalidCredentials();
        }

        var now = clock.UtcNow;

        if (storedToken.RevokedAt.HasValue)
        {
            var family = await dbContext.RefreshTokens
                .Where(candidate => candidate.FamilyId == storedToken.FamilyId && candidate.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var familyToken in family)
            {
                familyToken.Revoke(now);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            await using var reuseTransaction = await BeginTransactionIfNeededAsync(
                dbContext,
                cancellationToken);
            await SetSessionVariablesAsync(
                dbContext,
                storedToken.UserId,
                isPlatformAdmin: false,
                storedToken.TenantId,
                cancellationToken);

            await auditWriter.RecordAsync(
                new AuditEntry(
                    storedToken.TenantId,
                    storedToken.UserId,
                    "auth.refresh.reuse_detected",
                    "RefreshToken",
                    storedToken.Id.ToString(),
                    CorrelationId: httpContext.TraceIdentifier,
                    IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
                cancellationToken);

            if (reuseTransaction is not null)
            {
                await reuseTransaction.CommitAsync(cancellationToken);
            }

            RefreshCookie.Delete(httpContext, cookieName);
            return InvalidCredentials();
        }

        if (!storedToken.IsActive(now))
        {
            RefreshCookie.Delete(httpContext, cookieName);
            return InvalidCredentials();
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == storedToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            storedToken.Revoke(now);
            await dbContext.SaveChangesAsync(cancellationToken);
            RefreshCookie.Delete(httpContext, cookieName);
            return InvalidCredentials();
        }

        await using var transaction = await BeginTransactionIfNeededAsync(dbContext, cancellationToken);
        await SetSessionVariablesAsync(
            dbContext,
            user.Id,
            user.IsPlatformAdmin,
            storedToken.TenantId,
            cancellationToken);

        Tenant? tenant = null;
        IReadOnlyList<string> permissions = [];

        if (storedToken.TenantId.HasValue)
        {
            tenant = await SessionQueries.LoadTenantAsync(
                dbContext,
                storedToken.TenantId.Value,
                cancellationToken);

            var membership = await SessionQueries.LoadMembershipAsync(
                dbContext,
                storedToken.TenantId.Value,
                user.Id,
                cancellationToken);

            if (tenant is null || !tenant.CanOperate || membership is null || !membership.IsActive)
            {
                storedToken.Revoke(now);
                await dbContext.SaveChangesAsync(cancellationToken);
                RefreshCookie.Delete(httpContext, cookieName);
                return InvalidCredentials();
            }

            permissions = SessionQueries.ResolvePermissions(membership);
        }

        if (user.IsPlatformAdmin)
        {
            permissions = permissions
                .Append(Permissions.PlatformTenantsManage)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        var issued = await sessionTokenService.CreateAsync(
            user,
            storedToken.TenantId,
            storedToken.FamilyId,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers.UserAgent.ToString(),
            cancellationToken,
            saveChanges: false);

        storedToken.Revoke(now, issued.RefreshTokenId);

        await auditWriter.RecordAsync(
            new AuditEntry(
                storedToken.TenantId,
                user.Id,
                "auth.refresh.succeeded",
                "RefreshToken",
                issued.RefreshTokenId.ToString(),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        RefreshCookie.Append(httpContext, cookieName, issued.RefreshToken, issued.RefreshTokenExpiresAt);

        return TypedResults.Ok(BuildSessionResponse(issued, user, tenant, permissions));
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        LendingDbContext dbContext,
        ISecureTokenGenerator tokenGenerator,
        IAuditWriter auditWriter,
        IClock clock,
        IOptions<AuthOptions> authOptions,
        CancellationToken cancellationToken)
    {
        var cookieName = authOptions.Value.RefreshCookieName;
        var rawToken = RefreshCookie.Read(httpContext, cookieName);

        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            var tokenHash = tokenGenerator.Hash(rawToken);
            var storedToken = await dbContext.RefreshTokens
                .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken);

            if (storedToken is not null && !storedToken.RevokedAt.HasValue)
            {
                storedToken.Revoke(clock.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);

                await using var logoutTransaction = await BeginTransactionIfNeededAsync(
                    dbContext,
                    cancellationToken);
                await SetSessionVariableAsync(
                    dbContext,
                    "app.user_id",
                    storedToken.UserId,
                    cancellationToken);

                if (storedToken.TenantId.HasValue)
                {
                    await SetSessionVariableAsync(
                        dbContext,
                        "app.tenant_id",
                        storedToken.TenantId.Value,
                        cancellationToken);
                }

                await auditWriter.RecordAsync(
                    new AuditEntry(
                        storedToken.TenantId,
                        storedToken.UserId,
                        "auth.logout",
                        "RefreshToken",
                        storedToken.Id.ToString(),
                        CorrelationId: httpContext.TraceIdentifier,
                        IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
                    cancellationToken);

                if (logoutTransaction is not null)
                {
                    await logoutTransaction.CommitAsync(cancellationToken);
                }
            }
        }

        RefreshCookie.Delete(httpContext, cookieName);
        return TypedResults.NoContent();
    }

    private static IResult GetMe(CurrentUser currentUser)
    {
        if (!currentUser.UserId.HasValue || currentUser.Email is null || currentUser.FullName is null)
        {
            return TypedResults.Unauthorized();
        }

        TenantSummary? tenant = null;
        if (currentUser.TenantId.HasValue &&
            currentUser.TenantName is not null &&
            currentUser.TenantSlug is not null)
        {
            tenant = new TenantSummary(
                currentUser.TenantId.Value,
                currentUser.TenantSlug,
                currentUser.TenantName);
        }

        return TypedResults.Ok(new MeResponse(
            new SessionUserResponse(
                currentUser.UserId.Value,
                currentUser.Email,
                currentUser.FullName,
                currentUser.IsPlatformAdmin),
            tenant,
            currentUser.Permissions.Order(StringComparer.Ordinal).ToArray()));
    }

    private static async Task<IResult> SwitchTenantAsync(
        SwitchTenantRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        ISecureTokenGenerator tokenGenerator,
        SessionTokenService sessionTokenService,
        IAuditWriter auditWriter,
        IClock clock,
        IOptions<AuthOptions> authOptions,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        var user = await dbContext.Users
            .SingleAsync(candidate => candidate.Id == currentUser.UserId.Value, cancellationToken);

        await using var transaction = await BeginTransactionIfNeededAsync(dbContext, cancellationToken);
        await SetSessionVariablesAsync(
            dbContext,
            user.Id,
            user.IsPlatformAdmin,
            request.TenantId,
            cancellationToken);

        var tenant = await SessionQueries.LoadTenantAsync(dbContext, request.TenantId, cancellationToken);
        var membership = await SessionQueries.LoadMembershipAsync(
            dbContext,
            request.TenantId,
            user.Id,
            cancellationToken);

        if (tenant is null || !tenant.CanOperate || membership is null || !membership.IsActive)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Sin acceso",
                detail: "No tienes una membresía activa en esta organización.");
        }

        var cookieName = authOptions.Value.RefreshCookieName;
        var rawToken = RefreshCookie.Read(httpContext, cookieName);
        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            var tokenHash = tokenGenerator.Hash(rawToken);
            var currentToken = await dbContext.RefreshTokens
                .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken);

            currentToken?.Revoke(clock.UtcNow);
        }

        IReadOnlyList<string> permissions = SessionQueries.ResolvePermissions(membership);
        if (user.IsPlatformAdmin)
        {
            permissions = permissions
                .Append(Permissions.PlatformTenantsManage)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        var issued = await sessionTokenService.CreateAsync(
            user,
            tenant.Id,
            familyId: null,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers.UserAgent.ToString(),
            cancellationToken,
            saveChanges: false);

        await auditWriter.RecordAsync(
            new AuditEntry(
                tenant.Id,
                user.Id,
                "auth.tenant_switched",
                "Tenant",
                tenant.Id.ToString(),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        RefreshCookie.Append(httpContext, cookieName, issued.RefreshToken, issued.RefreshTokenExpiresAt);

        return TypedResults.Ok(BuildSessionResponse(issued, user, tenant, permissions));
    }

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        HttpContext httpContext,
        LendingDbContext dbContext,
        ISecureTokenGenerator tokenGenerator,
        IEmailSender emailSender,
        IAuditWriter auditWriter,
        IClock clock,
        IOptions<AuthOptions> authOptions,
        CancellationToken cancellationToken)
    {
        Email email;
        try
        {
            email = Email.Create(request.Email);
        }
        catch (DomainException)
        {
            return Results.Json(
                new MessageResponse(
                    "Si el correo existe, enviamos un enlace para restablecer la contraseña."),
                statusCode: StatusCodes.Status202Accepted);
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is not null && user.IsActive)
        {
            var now = clock.UtcNow;
            var generated = tokenGenerator.Generate();
            var resetToken = PasswordResetToken.Create(
                user.Id,
                generated.Hash,
                now,
                now.AddMinutes(authOptions.Value.PasswordResetMinutes));

            dbContext.PasswordResetTokens.Add(resetToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await using var resetTransaction = await BeginTransactionIfNeededAsync(
                dbContext,
                cancellationToken);
            await SetSessionVariableAsync(dbContext, "app.user_id", user.Id, cancellationToken);

            await auditWriter.RecordAsync(
                new AuditEntry(
                    null,
                    user.Id,
                    "auth.password_reset.requested",
                    "User",
                    user.Id.ToString(),
                    CorrelationId: httpContext.TraceIdentifier,
                    IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
                cancellationToken);

            if (resetTransaction is not null)
            {
                await resetTransaction.CommitAsync(cancellationToken);
            }

            var link = $"{authOptions.Value.PublicUrl.TrimEnd('/')}/restablecer?token={Uri.EscapeDataString(generated.Token)}";
            await emailSender.SendAsync(
                new EmailMessage(
                    user.Email.Value,
                    "Restablece tu contraseña",
                    $"Usa este enlace para restablecer tu contraseña: {link}. Expira en {authOptions.Value.PasswordResetMinutes} minutos."),
                cancellationToken);
        }

        return Results.Json(
            new MessageResponse(
                "Si el correo existe, enviamos un enlace para restablecer la contraseña."),
            statusCode: StatusCodes.Status202Accepted);
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        HttpContext httpContext,
        LendingDbContext dbContext,
        ISecureTokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        IAuditWriter auditWriter,
        IClock clock,
        PlatformSettingsStore settingsStore,
        CancellationToken cancellationToken)
    {
        var securityPolicy = await settingsStore.GetSecurityPolicyAsync(cancellationToken);
        var validationError = ValidatePassword(request.NewPassword, securityPolicy);
        if (validationError is not null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Contraseña inválida",
                detail: validationError);
        }

        var tokenHash = tokenGenerator.Hash(request.Token);
        var resetToken = await dbContext.PasswordResetTokens
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken);

        var now = clock.UtcNow;
        if (resetToken is null || !resetToken.IsActive(now))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Enlace inválido",
                detail: "El enlace expiró o ya fue utilizado.");
        }

        var user = await dbContext.Users
            .SingleAsync(candidate => candidate.Id == resetToken.UserId, cancellationToken);

        user.SetPassword(passwordHasher.Hash(request.NewPassword), now);
        resetToken.MarkUsed(now);

        var activeTokens = await dbContext.RefreshTokens
            .Where(candidate => candidate.UserId == user.Id && candidate.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            activeToken.Revoke(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await using var resetTransaction = await BeginTransactionIfNeededAsync(
            dbContext,
            cancellationToken);
        await SetSessionVariableAsync(dbContext, "app.user_id", user.Id, cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                null,
                user.Id,
                "auth.password_reset.completed",
                "User",
                user.Id.ToString(),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        if (resetTransaction is not null)
        {
            await resetTransaction.CommitAsync(cancellationToken);
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> AcceptInvitationAsync(
        AcceptInvitationRequest request,
        HttpContext httpContext,
        LendingDbContext dbContext,
        ISecureTokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        SessionTokenService sessionTokenService,
        IAuditWriter auditWriter,
        IClock clock,
        PlatformSettingsStore settingsStore,
        IOptions<AuthOptions> authOptions,
        CancellationToken cancellationToken)
    {
        TenantSlug slug;
        try
        {
            slug = TenantSlug.Create(request.TenantSlug);
        }
        catch (DomainException)
        {
            return InvalidInvitation();
        }

        var tenant = await dbContext.Tenants
            .SingleOrDefaultAsync(candidate => candidate.Slug == slug, cancellationToken);

        if (tenant is null || !tenant.CanOperate)
        {
            return InvalidInvitation();
        }

        await using var transaction = await BeginTransactionIfNeededAsync(dbContext, cancellationToken);
        await SetSessionVariableAsync(dbContext, "app.tenant_id", tenant.Id, cancellationToken);

        var tokenHash = tokenGenerator.Hash(request.Token);
        var invitation = await dbContext.Invitations
            .Include(candidate => candidate.Roles)
            .SingleOrDefaultAsync(
                candidate => candidate.TenantId == tenant.Id && candidate.TokenHash == tokenHash,
                cancellationToken);

        var now = clock.UtcNow;
        if (invitation is null || !invitation.IsActive(now))
        {
            return InvalidInvitation();
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Email == invitation.Email, cancellationToken);

        if (user is null)
        {
            var securityPolicy = await settingsStore.GetSecurityPolicyAsync(cancellationToken);
            var validationError = ValidatePassword(
                request.Password,
                securityPolicy);

            if (validationError is not null)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Contraseña inválida",
                    detail: validationError);
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Nombre requerido",
                    detail: "Ingresa tu nombre completo.");
            }

            user = User.Create(
                invitation.Email,
                request.FullName,
                passwordHasher.Hash(request.Password),
                now);
            user.VerifyEmail(now);
            dbContext.Users.Add(user);
        }
        else
        {
            if (!user.IsActive)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Cuenta suspendida",
                    detail: "Contacta al administrador de la plataforma.");
            }

            if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                return InvalidCredentials();
            }
        }

        await SetSessionVariableAsync(dbContext, "app.user_id", user.Id, cancellationToken);

        var roleCodes = invitation.Roles.Select(role => role.RoleCode).Distinct(StringComparer.Ordinal).ToArray();
        var roles = await dbContext.Roles
            .Where(role => role.TenantId == tenant.Id && roleCodes.Contains(role.Code))
            .ToListAsync(cancellationToken);

        if (roles.Count != roleCodes.Length)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invitación inválida",
                detail: "La invitación hace referencia a roles que ya no existen.");
        }

        var roleIds = roles.Select(role => role.Id).ToArray();
        var membership = await dbContext.TenantMemberships
            .Include(candidate => candidate.Roles)
            .SingleOrDefaultAsync(
                candidate => candidate.TenantId == tenant.Id && candidate.UserId == user.Id,
                cancellationToken);

        if (membership is null)
        {
            membership = TenantMembership.CreateActive(tenant.Id, user.Id, roleIds, now);
            dbContext.TenantMemberships.Add(membership);
        }
        else
        {
            membership.ReplaceRoles(roleIds, now);
            membership.Activate(now);
        }

        invitation.Accept(now);
        await dbContext.SaveChangesAsync(cancellationToken);

        var loadedMembership = await SessionQueries.LoadMembershipAsync(
            dbContext,
            tenant.Id,
            user.Id,
            cancellationToken);

        if (loadedMembership is null)
        {
            return InvalidInvitation();
        }

        IReadOnlyList<string> permissions = SessionQueries.ResolvePermissions(loadedMembership);
        if (user.IsPlatformAdmin)
        {
            permissions = permissions
                .Append(Permissions.PlatformTenantsManage)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        var issued = await sessionTokenService.CreateAsync(
            user,
            tenant.Id,
            familyId: null,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers.UserAgent.ToString(),
            cancellationToken,
            saveChanges: false);

        await auditWriter.RecordAsync(
            new AuditEntry(
                tenant.Id,
                user.Id,
                "auth.invitation.accepted",
                "Invitation",
                invitation.Id.ToString(),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        RefreshCookie.Append(
            httpContext,
            authOptions.Value.RefreshCookieName,
            issued.RefreshToken,
            issued.RefreshTokenExpiresAt);

        return TypedResults.Ok(BuildSessionResponse(issued, user, tenant, permissions));
    }

    private static SessionResponse BuildSessionResponse(
        IssuedSession issued,
        User user,
        Tenant? tenant,
        IReadOnlyList<string> permissions) =>
        new(
            issued.AccessToken,
            issued.AccessTokenExpiresAt,
            new SessionUserResponse(user.Id, user.Email.Value, user.FullName, user.IsPlatformAdmin),
            tenant is null ? null : new TenantSummary(tenant.Id, tenant.Slug.Value, tenant.Name),
            permissions);

    private static async Task<IDbContextTransaction?> BeginTransactionIfNeededAsync(
        LendingDbContext dbContext,
        CancellationToken cancellationToken) =>
        dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

    private static Task<int> SetSessionVariableAsync(
        LendingDbContext dbContext,
        string variable,
        Guid value,
        CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config({variable}, {value.ToString()}, true)",
            cancellationToken);

    private static Task<int> SetSessionVariablesAsync(
        LendingDbContext dbContext,
        Guid userId,
        bool isPlatformAdmin,
        Guid? tenantId,
        CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.user_id', {userId.ToString()}, true), set_config('app.is_platform_admin', {(isPlatformAdmin ? "true" : "false")}, true), set_config('app.tenant_id', {tenantId?.ToString() ?? string.Empty}, true)",
            cancellationToken);

    private static ProblemHttpResult InvalidCredentials() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Credenciales inválidas",
            detail: "El correo o la contraseña no son correctos.");

    private static ProblemHttpResult NoMembership() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Sin organización",
            detail: "Tu cuenta no tiene una membresía activa. Solicita una invitación.");

    private static ProblemHttpResult InvalidInvitation() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invitación inválida",
            detail: "La invitación expiró, fue revocada o ya fue utilizada.");

    private static string? ValidatePassword(
        string password,
        PlatformSecurityPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < policy.MinPasswordLength)
        {
            return $"La contraseña debe tener al menos {policy.MinPasswordLength} caracteres.";
        }

        if (policy.RequireLetterAndDigit &&
            (!password.Any(char.IsLetter) || !password.Any(char.IsDigit)))
        {
            return "La contraseña debe incluir al menos una letra y un número.";
        }

        if (password.Contains(' ', StringComparison.Ordinal))
        {
            return "La contraseña no puede contener espacios.";
        }

        return null;
    }

    public sealed record LoginRequest(string Email, string Password, string? TenantSlug);

    public sealed record LoginResponse(
        bool RequiresTenantSelection,
        SessionUserResponse? User,
        IReadOnlyList<TenantOption>? Tenants,
        SessionResponse? Session);

    public sealed record TenantOption(Guid Id, string Slug, string Name);

    public sealed record SessionResponse(
        string AccessToken,
        DateTimeOffset ExpiresAt,
        SessionUserResponse User,
        TenantSummary? Tenant,
        IReadOnlyList<string> Permissions);

    public sealed record SessionUserResponse(Guid Id, string Email, string FullName, bool IsPlatformAdmin);

    public sealed record TenantSummary(Guid Id, string Slug, string Name);

    public sealed record MeResponse(
        SessionUserResponse User,
        TenantSummary? Tenant,
        IReadOnlyList<string> Permissions);

    public sealed record SwitchTenantRequest(Guid TenantId);

    public sealed record ForgotPasswordRequest(string Email);

    public sealed record ResetPasswordRequest(string Token, string NewPassword);

    public sealed record AcceptInvitationRequest(
        string Token,
        string TenantSlug,
        string FullName,
        string Password);

    public sealed record MessageResponse(string Message);
}
