using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Claims;
using Lending.Api.Authentication;
using Lending.Api.Authorization;
using Lending.Api.Infrastructure;
using Lending.Application.Abstractions;
using Lending.Domain.Common;
using Lending.Domain.Identity;
using Lending.Domain.Platform;
using Lending.Infrastructure.Email;
using Lending.Infrastructure.Persistence;
using Lending.Infrastructure.Platform;
using Lending.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Lending.Api.Configuration;

namespace Lending.Api.Endpoints;

public static class PlatformSettingsEndpoints
{
    private const string PlatformPermission = Permissions.PlatformTenantsManage;

    public static IEndpointRouteBuilder MapPlatformSettingsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/platform/settings")
            .WithTags("Platform Settings")
            .RequireAuthorization();

        group.MapGet("/profile", GetProfileAsync)
            .WithName("GetPlatformProfile")
            .RequirePermission(PlatformPermission);

        group.MapPut("/profile", UpdateProfileAsync)
            .WithName("UpdatePlatformProfile")
            .RequirePermission(PlatformPermission);

        group.MapPost("/profile/change-password", ChangePasswordAsync)
            .WithName("ChangePlatformPassword")
            .RequirePermission(PlatformPermission)
            .RequireRateLimiting("auth");

        group.MapGet("/security", GetSecurityAsync)
            .WithName("GetPlatformSecurity")
            .RequirePermission(PlatformPermission);

        group.MapPut("/security", UpdateSecurityAsync)
            .WithName("UpdatePlatformSecurity")
            .RequirePermission(PlatformPermission);

        group.MapGet("/security/sessions", GetSessionsAsync)
            .WithName("GetPlatformSessions")
            .RequirePermission(PlatformPermission);

        group.MapDelete("/security/sessions/{familyId:guid}", RevokeSessionAsync)
            .WithName("RevokePlatformSession")
            .RequirePermission(PlatformPermission);

        group.MapDelete("/security/sessions", RevokeOtherSessionsAsync)
            .WithName("RevokeOtherPlatformSessions")
            .RequirePermission(PlatformPermission);

        group.MapGet("/security/audit", GetSecurityAuditAsync)
            .WithName("GetPlatformSecurityAudit")
            .RequirePermission(PlatformPermission);

        group.MapGet("/email", GetEmailAsync)
            .WithName("GetPlatformEmailSettings")
            .RequirePermission(PlatformPermission);

        group.MapPut("/email", UpdateEmailAsync)
            .WithName("UpdatePlatformEmailSettings")
            .RequirePermission(PlatformPermission);

        group.MapPost("/email/check", CheckEmailConnectionAsync)
            .WithName("CheckPlatformEmailConnection")
            .RequirePermission(PlatformPermission);

        group.MapPost("/email/test", SendTestEmailAsync)
            .WithName("SendPlatformTestEmail")
            .RequirePermission(PlatformPermission)
            .RequireRateLimiting("auth");

        group.MapGet("/notifications", GetNotificationsAsync)
            .WithName("GetPlatformNotifications")
            .RequirePermission(PlatformPermission);

        group.MapPost("/notifications/{id:guid}/read", MarkNotificationReadAsync)
            .WithName("MarkPlatformNotificationRead")
            .RequirePermission(PlatformPermission);

        group.MapPost("/notifications/read-all", MarkAllNotificationsReadAsync)
            .WithName("MarkAllPlatformNotificationsRead")
            .RequirePermission(PlatformPermission);

        group.MapGet("/notifications/preferences", GetNotificationPreferencesAsync)
            .WithName("GetPlatformNotificationPreferences")
            .RequirePermission(PlatformPermission);

        group.MapPut("/notifications/preferences", UpdateNotificationPreferencesAsync)
            .WithName("UpdatePlatformNotificationPreferences")
            .RequirePermission(PlatformPermission);

        group.MapGet("/system/status", GetSystemStatusAsync)
            .WithName("GetPlatformSystemStatus")
            .RequirePermission(PlatformPermission);

        return endpoints;
    }

    private static async Task<IResult> GetProfileAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == currentUser.UserId.Value,
                cancellationToken);

        return user is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToProfile(user));
    }

    private static async Task<IResult> UpdateProfileAsync(
        UpdateProfileRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Id == currentUser.UserId.Value,
                cancellationToken);

        if (user is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            user.Rename(request.FullName, clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Datos inválidos",
                detail: exception.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                TenantId: null,
                ActorUserId: user.Id,
                Action: "platform.profile.updated",
                EntityType: "User",
                EntityId: user.Id.ToString(),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        return TypedResults.Ok(ToProfile(user));
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IPasswordHasher passwordHasher,
        PlatformSettingsStore settingsStore,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Invalid("Ingresa la contraseña actual y la nueva contraseña.");
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Id == currentUser.UserId.Value,
                cancellationToken);

        if (user is null)
        {
            return TypedResults.NotFound();
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Contraseña incorrecta",
                detail: "La contraseña actual no es correcta.");
        }

        var policy = await settingsStore.GetSecurityPolicyAsync(cancellationToken);
        var validationError = ValidatePassword(request.NewPassword, policy);
        if (validationError is not null)
        {
            return Invalid(validationError);
        }

        if (passwordHasher.Verify(request.NewPassword, user.PasswordHash))
        {
            return Invalid("La nueva contraseña debe ser diferente a la actual.");
        }

        var now = clock.UtcNow;
        user.SetPassword(passwordHasher.Hash(request.NewPassword), now);

        var activeTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == user.Id && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                TenantId: null,
                ActorUserId: user.Id,
                Action: "platform.password.changed",
                EntityType: "User",
                EntityId: user.Id.ToString(),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        return TypedResults.Ok(new MessageResponse(
            "Contraseña actualizada. Todas las sesiones fueron cerradas; inicia sesión nuevamente."));
    }

    private static async Task<IResult> GetSecurityAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        PlatformSettingsStore settingsStore,
        IOptions<JwtOptions> jwtOptions,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        var settings = await settingsStore.GetAsync(cancellationToken);
        var activeSessions = await dbContext.RefreshTokens
            .Where(token => token.UserId == currentUser.UserId.Value
                && token.RevokedAt == null
                && token.ExpiresAt > clock.UtcNow)
            .Select(token => token.FamilyId)
            .Distinct()
            .CountAsync(cancellationToken);

        return TypedResults.Ok(new SecuritySettingsResponse(
            new SecurityPolicyResponse(
                settings.MinPasswordLength,
                settings.RequireLetterAndDigit,
                settings.MaxFailedAttempts,
                settings.LockoutMinutes,
                settings.RefreshTokenDays),
            jwtOptions.Value.AccessTokenMinutes,
            activeSessions,
            TwoFactorAvailable: false,
            TwoFactorReason: "La autenticación de dos factores todavía no está implementada en la plataforma.",
            LastPasswordChangeAt: null));
    }

    private static async Task<IResult> UpdateSecurityAsync(
        UpdateSecurityRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        PlatformSettingsStore settingsStore,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var settings = await settingsStore.GetAsync(cancellationToken);
        var before = new
        {
            settings.MinPasswordLength,
            settings.RequireLetterAndDigit,
            settings.MaxFailedAttempts,
            settings.LockoutMinutes,
            settings.RefreshTokenDays,
        };

        try
        {
            settings.UpdateSecurity(
                request.MinPasswordLength,
                request.RequireLetterAndDigit,
                request.MaxFailedAttempts,
                request.LockoutMinutes,
                request.RefreshTokenDays,
                clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return Invalid(exception.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                TenantId: null,
                ActorUserId: currentUser.UserId,
                Action: "platform.security.settings_updated",
                EntityType: "PlatformSettings",
                EntityId: settings.Id.ToString(),
                BeforeJson: System.Text.Json.JsonSerializer.Serialize(before),
                AfterJson: System.Text.Json.JsonSerializer.Serialize(new
                {
                    settings.MinPasswordLength,
                    settings.RequireLetterAndDigit,
                    settings.MaxFailedAttempts,
                    settings.LockoutMinutes,
                    settings.RefreshTokenDays,
                }),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        return TypedResults.Ok(new SecurityPolicyResponse(
            settings.MinPasswordLength,
            settings.RequireLetterAndDigit,
            settings.MaxFailedAttempts,
            settings.LockoutMinutes,
            settings.RefreshTokenDays));
    }

    private static async Task<IResult> GetSessionsAsync(
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        var now = clock.UtcNow;
        var currentFamily = GetCurrentFamilyId(httpContext);
        var sessions = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.UserId == currentUser.UserId.Value
                && token.RevokedAt == null
                && token.ExpiresAt > now)
            .OrderByDescending(token => token.CreatedAt)
            .ToListAsync(cancellationToken);

        var tenantIds = sessions
            .Where(token => token.TenantId.HasValue)
            .Select(token => token.TenantId!.Value)
            .Distinct()
            .ToArray();

        var tenantNames = await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenantIds.Contains(tenant.Id))
            .ToDictionaryAsync(tenant => tenant.Id, tenant => tenant.Name, cancellationToken);

        var result = sessions
            .GroupBy(token => token.FamilyId)
            .Select(group => group.OrderByDescending(token => token.CreatedAt).First())
            .Select(token => new SessionSummary(
                token.FamilyId,
                token.CreatedAt,
                token.ExpiresAt,
                token.TenantId.HasValue
                    ? tenantNames.GetValueOrDefault(token.TenantId.Value)
                    : null,
                token.UserAgent,
                !string.IsNullOrWhiteSpace(token.CreatedByIpHash),
                token.FamilyId == currentFamily))
            .ToArray();

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RevokeSessionAsync(
        Guid familyId,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        var tokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == currentUser.UserId.Value
                && token.FamilyId == familyId
                && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            return TypedResults.NotFound();
        }

        var now = clock.UtcNow;
        foreach (var token in tokens)
        {
            token.Revoke(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                TenantId: null,
                ActorUserId: currentUser.UserId,
                Action: "platform.session.revoked",
                EntityType: "RefreshToken",
                EntityId: familyId.ToString(),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> RevokeOtherSessionsAsync(
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        var currentFamily = GetCurrentFamilyId(httpContext);
        var tokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == currentUser.UserId.Value
                && token.RevokedAt == null
                && token.FamilyId != currentFamily)
            .ToListAsync(cancellationToken);

        var now = clock.UtcNow;
        foreach (var token in tokens)
        {
            token.Revoke(now);
        }

        if (tokens.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditWriter.RecordAsync(
                new AuditEntry(
                    TenantId: null,
                    ActorUserId: currentUser.UserId,
                    Action: "platform.sessions.revoked_all_other",
                    EntityType: "RefreshToken",
                    EntityId: currentUser.UserId.Value.ToString(),
                    CorrelationId: httpContext.TraceIdentifier,
                    IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
                cancellationToken);
        }

        return TypedResults.Ok(new RevokedSessionsResponse(tokens.Count));
    }

    private static async Task<IResult> GetSecurityAuditAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        var events = await dbContext.AuditEvents
            .AsNoTracking()
            .Where(auditEvent => auditEvent.ActorUserId == currentUser.UserId.Value
                && (auditEvent.Action.StartsWith("auth.")
                    || auditEvent.Action.StartsWith("platform.")
                    || auditEvent.Action.StartsWith("security.")))
            .OrderByDescending(auditEvent => auditEvent.OccurredAt)
            .Take(50)
            .Select(auditEvent => new SecurityAuditEntry(
                auditEvent.Action,
                auditEvent.EntityType,
                auditEvent.EntityId,
                auditEvent.OccurredAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(events);
    }

    private static async Task<IResult> GetEmailAsync(
        PlatformSettingsStore settingsStore,
        CancellationToken cancellationToken)
    {
        var settings = await settingsStore.GetAsync(cancellationToken);
        return TypedResults.Ok(ToEmailResponse(settings));
    }

    private static async Task<IResult> UpdateEmailAsync(
        UpdateEmailRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        PlatformSettingsStore settingsStore,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var settings = await settingsStore.GetAsync(cancellationToken);

        string? fromAddress = null;
        if (!string.IsNullOrWhiteSpace(request.FromAddress))
        {
            try
            {
                fromAddress = Email.Create(request.FromAddress).Value;
            }
            catch (DomainException exception)
            {
                return Invalid(exception.Message);
            }
        }

        try
        {
            settings.UpdateEmail(
                request.Enabled,
                request.Host,
                request.Port,
                request.UseSsl,
                request.Username,
                fromAddress,
                request.FromName,
                clock.UtcNow);

            if (request.ClearPassword)
            {
                settings.SetEmailPassword(null, clock.UtcNow);
            }
            else if (!string.IsNullOrWhiteSpace(request.SmtpPassword))
            {
                settings.SetEmailPassword(
                    settingsStore.ProtectEmailPassword(request.SmtpPassword),
                    clock.UtcNow);
            }
        }
        catch (DomainException exception)
        {
            return Invalid(exception.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                TenantId: null,
                ActorUserId: currentUser.UserId,
                Action: "platform.email.settings_updated",
                EntityType: "PlatformSettings",
                EntityId: settings.Id.ToString(),
                AfterJson: System.Text.Json.JsonSerializer.Serialize(new
                {
                    settings.EmailEnabled,
                    settings.EmailHost,
                    settings.EmailPort,
                    settings.EmailUseSsl,
                    settings.EmailUsername,
                    settings.EmailFromAddress,
                    settings.EmailFromName,
                    hasPassword = settings.HasEmailPassword,
                }),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        return TypedResults.Ok(ToEmailResponse(settings));
    }

    private static async Task<IResult> CheckEmailConnectionAsync(
        PlatformSettingsStore settingsStore,
        CancellationToken cancellationToken)
    {
        var configuration = await settingsStore.GetEmailConfigurationAsync(cancellationToken);
        if (!configuration.Enabled ||
            string.IsNullOrWhiteSpace(configuration.Host))
        {
            return Invalid("Configura y habilita el servidor SMTP antes de comprobar la conexión.");
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            using var client = new TcpClient();
            await client.ConnectAsync(
                configuration.Host,
                configuration.Port,
                timeout.Token);

            stopwatch.Stop();

            return TypedResults.Ok(new EmailConnectionResponse(
                Reachable: true,
                LatencyMs: (int)stopwatch.ElapsedMilliseconds,
                Error: null));
        }
        catch (Exception exception) when (exception is SocketException or OperationCanceledException)
        {
            stopwatch.Stop();
            var error = exception is OperationCanceledException
                ? "La conexión excedió 5 segundos."
                : exception.Message;

            return TypedResults.Ok(new EmailConnectionResponse(
                Reachable: false,
                LatencyMs: (int)stopwatch.ElapsedMilliseconds,
                Error: error));
        }
    }

    private static async Task<IResult> SendTestEmailAsync(
        TestEmailRequest? request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        PlatformSettingsStore settingsStore,
        SmtpEmailSender emailSender,
        PlatformNotificationService notificationService,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var settings = await settingsStore.GetAsync(cancellationToken);
        var configuration = await settingsStore.GetEmailConfigurationAsync(cancellationToken);

        if (!configuration.IsReady)
        {
            return Invalid("Guarda una configuración SMTP completa y habilitada antes de enviar una prueba.");
        }

        var recipient = string.IsNullOrWhiteSpace(request?.To)
            ? currentUser.Email
            : request.To;

        if (string.IsNullOrWhiteSpace(recipient))
        {
            return Invalid("No se pudo determinar el destinatario de la prueba.");
        }

        try
        {
            var validRecipient = Email.Create(recipient).Value;
            await emailSender.SendTestAsync(
                new EmailMessage(
                    validRecipient,
                    "Prueba de configuración de correo",
                    "Este es un correo de prueba enviado desde la configuración del Super Administrador.\n\n"
                    + "Si lo recibiste, la configuración SMTP funciona correctamente."),
                cancellationToken);

            settings.RecordEmailTest(clock.UtcNow, succeeded: true, error: null);
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditWriter.RecordAsync(
                new AuditEntry(
                    TenantId: null,
                    ActorUserId: currentUser.UserId,
                    Action: "platform.email.test_succeeded",
                    EntityType: "PlatformSettings",
                    EntityId: settings.Id.ToString(),
                    CorrelationId: httpContext.TraceIdentifier,
                    IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
                cancellationToken);

            return TypedResults.Ok(new EmailTestResponse(
                Succeeded: true,
                Recipient: validRecipient,
                Error: null,
                TestedAt: settings.EmailLastTestAt));
        }
        catch (Exception exception)
        {
            settings.RecordEmailTest(clock.UtcNow, succeeded: false, error: exception.Message);
            await dbContext.SaveChangesAsync(cancellationToken);

            await notificationService.NotifyEmailFailureAsync(exception.Message, cancellationToken);

            await auditWriter.RecordAsync(
                new AuditEntry(
                    TenantId: null,
                    ActorUserId: currentUser.UserId,
                    Action: "platform.email.test_failed",
                    EntityType: "PlatformSettings",
                    EntityId: settings.Id.ToString(),
                    AfterJson: System.Text.Json.JsonSerializer.Serialize(new { error = exception.Message }),
                    CorrelationId: httpContext.TraceIdentifier,
                    IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
                cancellationToken);

            return TypedResults.Ok(new EmailTestResponse(
                Succeeded: false,
                Recipient: recipient,
                Error: exception.Message,
                TestedAt: settings.EmailLastTestAt));
        }
    }

    private static async Task<IResult> GetNotificationsAsync(
        bool? unreadOnly,
        LendingDbContext dbContext,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var query = dbContext.PlatformNotifications.AsNoTracking();

        if (unreadOnly == true)
        {
            query = query.Where(notification => !notification.IsRead);
        }

        var notifications = await query
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(100)
            .Select(notification => new NotificationResponse(
                notification.Id,
                notification.Title,
                notification.Message,
                notification.Category,
                notification.Severity,
                notification.LinkUrl,
                notification.IsRead,
                notification.CreatedAt,
                notification.ReadAt))
            .ToListAsync(cancellationToken);

        var unreadCount = await dbContext.PlatformNotifications
            .CountAsync(notification => !notification.IsRead, cancellationToken);

        return TypedResults.Ok(new NotificationsResponse(
            notifications,
            unreadCount,
            notifications.Count));
    }

    private static async Task<IResult> MarkNotificationReadAsync(
        Guid id,
        LendingDbContext dbContext,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var notification = await dbContext.PlatformNotifications
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (notification is null)
        {
            return TypedResults.NotFound();
        }

        notification.MarkRead(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> MarkAllNotificationsReadAsync(
        LendingDbContext dbContext,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var unread = await dbContext.PlatformNotifications
            .Where(notification => !notification.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.MarkRead(now);
        }

        if (unread.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return TypedResults.Ok(new MarkAllNotificationsResponse(unread.Count));
    }

    private static async Task<IResult> GetNotificationPreferencesAsync(
        PlatformSettingsStore settingsStore,
        CancellationToken cancellationToken)
    {
        var settings = await settingsStore.GetAsync(cancellationToken);
        return TypedResults.Ok(ToNotificationPreferences(settings));
    }

    private static async Task<IResult> UpdateNotificationPreferencesAsync(
        UpdateNotificationPreferencesRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        PlatformSettingsStore settingsStore,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var settings = await settingsStore.GetAsync(cancellationToken);

        settings.UpdateNotificationPreferences(
            request.TenantCreated,
            request.TenantSuspended,
            request.TenantReactivated,
            request.SuspiciousAccess,
            request.EmailFailures,
            clock.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                TenantId: null,
                ActorUserId: currentUser.UserId,
                Action: "platform.notifications.preferences_updated",
                EntityType: "PlatformSettings",
                EntityId: settings.Id.ToString(),
                AfterJson: System.Text.Json.JsonSerializer.Serialize(ToNotificationPreferences(settings)),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        return TypedResults.Ok(ToNotificationPreferences(settings));
    }

    private static async Task<IResult> GetSystemStatusAsync(
        LendingDbContext dbContext,
        PlatformSettingsStore settingsStore,
        ServerRuntimeInfo runtimeInfo,
        IHostEnvironment environment,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

        var uptime = clock.UtcNow - runtimeInfo.StartedAt;

        var databaseStopwatch = Stopwatch.StartNew();
        var databaseOperational = true;
        string? databaseError = null;

        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
        }
        catch (Exception exception)
        {
            databaseOperational = false;
            databaseError = exception.Message;
        }

        databaseStopwatch.Stop();

        var emailSettings = await settingsStore.GetAsync(cancellationToken);
        var emailState = DescribeEmailState(emailSettings);

        return TypedResults.Ok(new SystemStatusResponse(
            Api: new ApiStatusResponse(
                Status: "operational",
                Environment: environment.EnvironmentName,
                Version: version,
                Runtime: Environment.Version.ToString(),
                StartedAt: runtimeInfo.StartedAt,
                UptimeSeconds: (long)uptime.TotalSeconds),
            Database: new DatabaseStatusResponse(
                Status: databaseOperational ? "operational" : "degraded",
                Provider: "PostgreSQL",
                LatencyMs: (int)databaseStopwatch.ElapsedMilliseconds,
                Error: databaseError),
            Email: new EmailStatusResponse(
                Status: emailState,
                LastTestAt: emailSettings.EmailLastTestAt,
                LastTestSucceeded: emailSettings.EmailLastTestSucceeded,
                LastTestError: emailSettings.EmailLastTestError),
            Storage: new UnavailableFeatureResponse(
                Configured: false,
                Reason: "La plataforma todavía no tiene un proveedor de almacenamiento de archivos configurado."),
            ErrorTracking: new UnavailableFeatureResponse(
                Configured: false,
                Reason: "La plataforma todavía no persiste errores en una tabla de monitoreo."),
            TwoFactor: new UnavailableFeatureResponse(
                Configured: false,
                Reason: "La autenticación de dos factores todavía no está implementada.")));
    }

    private static ProfileResponse ToProfile(User user) =>
        new(
            user.Id,
            user.Email.Value,
            user.FullName,
            user.IsPlatformAdmin,
            user.Status.ToString(),
            user.EmailVerifiedAt,
            user.LastLoginAt,
            user.CreatedAt,
            user.UpdatedAt,
            AvatarAvailable: false);

    private static EmailSettingsResponse ToEmailResponse(PlatformSettings settings) =>
        new(
            settings.EmailEnabled,
            settings.EmailHost,
            settings.EmailPort,
            settings.EmailUseSsl,
            settings.EmailUsername,
            settings.HasEmailPassword,
            settings.EmailFromAddress,
            settings.EmailFromName,
            settings.EmailLastTestAt,
            settings.EmailLastTestSucceeded,
            settings.EmailLastTestError,
            DescribeEmailState(settings));

    private static NotificationPreferencesResponse ToNotificationPreferences(
        PlatformSettings settings) =>
        new(
            settings.NotifyTenantCreated,
            settings.NotifyTenantSuspended,
            settings.NotifyTenantReactivated,
            settings.NotifySuspiciousAccess,
            settings.NotifyEmailFailures);

    private static string DescribeEmailState(PlatformSettings settings)
    {
        if (!settings.EmailEnabled)
        {
            return "disabled";
        }

        if (string.IsNullOrWhiteSpace(settings.EmailHost) ||
            string.IsNullOrWhiteSpace(settings.EmailFromAddress))
        {
            return "incomplete";
        }

        if (settings.EmailLastTestSucceeded == true)
        {
            return "verified";
        }

        if (settings.EmailLastTestSucceeded == false)
        {
            return "error";
        }

        return "configured";
    }

    private static Guid? GetCurrentFamilyId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue("sid");
        return Guid.TryParse(value, out var familyId) ? familyId : null;
    }

    private static string? ValidatePassword(
        string password,
        PlatformSecurityPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            password.Length < policy.MinPasswordLength)
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

    private static ProblemHttpResult Invalid(string detail) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Datos inválidos",
            detail: detail);

    public sealed record UpdateProfileRequest(string FullName);

    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    public sealed record UpdateSecurityRequest(
        int MinPasswordLength,
        bool RequireLetterAndDigit,
        int MaxFailedAttempts,
        int LockoutMinutes,
        int RefreshTokenDays);

    public sealed record UpdateEmailRequest(
        bool Enabled,
        string? Host,
        int Port,
        bool UseSsl,
        string? Username,
        string? SmtpPassword,
        bool ClearPassword,
        string? FromAddress,
        string? FromName);

    public sealed record TestEmailRequest(string? To);

    public sealed record UpdateNotificationPreferencesRequest(
        bool TenantCreated,
        bool TenantSuspended,
        bool TenantReactivated,
        bool SuspiciousAccess,
        bool EmailFailures);

    public sealed record ProfileResponse(
        Guid Id,
        string Email,
        string FullName,
        bool IsPlatformAdmin,
        string Status,
        DateTimeOffset? EmailVerifiedAt,
        DateTimeOffset? LastLoginAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        bool AvatarAvailable);

    public sealed record SecurityPolicyResponse(
        int MinPasswordLength,
        bool RequireLetterAndDigit,
        int MaxFailedAttempts,
        int LockoutMinutes,
        int RefreshTokenDays);

    public sealed record SecuritySettingsResponse(
        SecurityPolicyResponse Security,
        int AccessTokenMinutes,
        int ActiveSessions,
        bool TwoFactorAvailable,
        string TwoFactorReason,
        DateTimeOffset? LastPasswordChangeAt);

    public sealed record SessionSummary(
        Guid FamilyId,
        DateTimeOffset CreatedAt,
        DateTimeOffset ExpiresAt,
        string? TenantName,
        string? UserAgent,
        bool HasIpHash,
        bool IsCurrent);

    public sealed record RevokedSessionsResponse(int RevokedCount);

    public sealed record SecurityAuditEntry(
        string Action,
        string EntityType,
        string? EntityId,
        DateTimeOffset OccurredAt);

    public sealed record EmailSettingsResponse(
        bool Enabled,
        string? Host,
        int Port,
        bool UseSsl,
        string? Username,
        bool HasPassword,
        string? FromAddress,
        string? FromName,
        DateTimeOffset? LastTestAt,
        bool? LastTestSucceeded,
        string? LastTestError,
        string Status);

    public sealed record EmailConnectionResponse(
        bool Reachable,
        int LatencyMs,
        string? Error);

    public sealed record EmailTestResponse(
        bool Succeeded,
        string Recipient,
        string? Error,
        DateTimeOffset? TestedAt);

    public sealed record NotificationResponse(
        Guid Id,
        string Title,
        string Message,
        string Category,
        string Severity,
        string? LinkUrl,
        bool IsRead,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ReadAt);

    public sealed record NotificationsResponse(
        IReadOnlyList<NotificationResponse> Items,
        int UnreadCount,
        int ReturnedCount);

    public sealed record MarkAllNotificationsResponse(int MarkedCount);

    public sealed record NotificationPreferencesResponse(
        bool TenantCreated,
        bool TenantSuspended,
        bool TenantReactivated,
        bool SuspiciousAccess,
        bool EmailFailures);

    public sealed record ApiStatusResponse(
        string Status,
        string Environment,
        string Version,
        string Runtime,
        DateTimeOffset StartedAt,
        long UptimeSeconds);

    public sealed record DatabaseStatusResponse(
        string Status,
        string Provider,
        int LatencyMs,
        string? Error);

    public sealed record EmailStatusResponse(
        string Status,
        DateTimeOffset? LastTestAt,
        bool? LastTestSucceeded,
        string? LastTestError);

    public sealed record UnavailableFeatureResponse(
        bool Configured,
        string Reason);

    public sealed record SystemStatusResponse(
        ApiStatusResponse Api,
        DatabaseStatusResponse Database,
        EmailStatusResponse Email,
        UnavailableFeatureResponse Storage,
        UnavailableFeatureResponse ErrorTracking,
        UnavailableFeatureResponse TwoFactor);

    public sealed record MessageResponse(string Message);
}
