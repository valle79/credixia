using System.Text.Json;
using Lending.Api.Authentication;
using Lending.Api.Authorization;
using Lending.Api.Configuration;
using Lending.Api.Infrastructure;
using Lending.Api.Tenancy;
using Lending.Application.Abstractions;
using Lending.Domain.Common;
using Lending.Domain.Identity;
using Lending.Domain.Tenancy;
using Lending.Domain.ValueObjects;
using Lending.Infrastructure.Audit;
using Lending.Infrastructure.Email;
using Lending.Infrastructure.Persistence;
using Lending.Infrastructure.Platform;
using Lending.Infrastructure.Security;
using Lending.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lending.Api.Endpoints;

public static class PlatformEndpoints
{
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/platform")
            .WithTags("Platform")
            .RequireAuthorization();

        group.MapGet("/tenants", GetTenantsAsync)
            .WithName("ListPlatformTenants")
            .RequirePermission(Permissions.PlatformTenantsManage);
        
        group.MapGet("/tenants/{id:guid}", GetTenantDetailsAsync)
            .WithName("GetPlatformTenantDetails")
            .RequirePermission(Permissions.PlatformTenantsManage);
        
        group.MapGet("/dashboard", GetDashboardAsync)
            .WithName("GetPlatformDashboard")
            .RequirePermission(Permissions.PlatformTenantsManage);

        group.MapPost("/tenants", CreateTenantAsync)
            .WithName("CreatePlatformTenant")
            .RequirePermission(Permissions.PlatformTenantsManage);

        group.MapPut("/tenants/{id:guid}", UpdateTenantAsync)
            .WithName("UpdatePlatformTenant")
            .RequirePermission(Permissions.PlatformTenantsManage);

        group.MapPatch("/tenants/{id:guid}/status", UpdateTenantStatusAsync)
            .WithName("UpdatePlatformTenantStatus")
            .RequirePermission(Permissions.PlatformTenantsManage);
        
        group.MapPost("/tenants/{id:guid}/send-credentials", SendCredentialsViaWhatsAppAsync)
            .WithName("SendTenantCredentialsViaWhatsApp")
            .RequirePermission(Permissions.PlatformTenantsManage);

        group.MapPost("/tenants/{id:guid}/reset-password", ResetOwnerPasswordAsync)
            .WithName("ResetTenantOwnerPassword")
            .RequirePermission(Permissions.PlatformTenantsManage);

        return endpoints;
    }

    private static async Task<IResult> GetTenantsAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsPlatformAdmin)
        {
            return PlatformAdminRequired();
        }

        var tenants = await dbContext.Tenants
            .AsNoTracking()
            .OrderBy(tenant => tenant.Name)
            .ToListAsync(cancellationToken);

        var memberCounts = await dbContext.TenantMemberships
            .AsNoTracking()
            .Where(membership => membership.Status == MembershipStatus.Active)
            .GroupBy(membership => membership.TenantId)
            .Select(group => new { TenantId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.TenantId, item => item.Count, cancellationToken);

        return TypedResults.Ok(tenants.Select(tenant => new PlatformTenantSummary(
            tenant.Id,
            tenant.Slug.Value,
            tenant.Name,
            tenant.Status.ToString(),
            memberCounts.GetValueOrDefault(tenant.Id),
            tenant.CreatedAt)).ToArray());
    }

    private static async Task<IResult> GetDashboardAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsPlatformAdmin)
        {
            return PlatformAdminRequired();
        }

        var tenantStatusCounts = await dbContext.Tenants
            .AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);

        var userStatusCounts = await dbContext.Users
            .AsNoTracking()
            .GroupBy(u => u.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);

        var totalTenants = tenantStatusCounts.Values.Sum();
        var activeTenants = tenantStatusCounts.GetValueOrDefault(TenantStatus.Active);
        var suspendedTenants = tenantStatusCounts.GetValueOrDefault(TenantStatus.Suspended);
        var totalUsers = userStatusCounts.Values.Sum();
        var activeUsers = userStatusCounts.GetValueOrDefault(UserStatus.Active);

        var totalMembers = await dbContext.TenantMemberships
            .AsNoTracking()
            .CountAsync(m => m.Status == MembershipStatus.Active, cancellationToken);

        var recentTenants = await dbContext.Tenants
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new RecentTenantDto(
                t.Id,
                t.Slug.Value,
                t.Name,
                t.Status.ToString(),
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new PlatformDashboardResponse(
            totalTenants,
            activeTenants,
            suspendedTenants,
            totalUsers,
            activeUsers,
            totalMembers,
            recentTenants));
    }

    private static async Task<IResult> SendCredentialsViaWhatsAppAsync(
        Guid id,
        SendCredentialsRequest request,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IOptions<AuthOptions> authOptions,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsPlatformAdmin)
        {
            return PlatformAdminRequired();
        }

        var tenant = await dbContext.Tenants
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (tenant is null)
        {
            return TypedResults.NotFound();
        }

        // Obtener el propietario de la organización
        var ownerMembership = await dbContext.TenantMemberships
            .Include(m => m.Roles)
            .ThenInclude(mr => mr.Role)
            .Where(m => m.TenantId == id 
                && m.Status == MembershipStatus.Active 
                && m.Roles.Any(r => r.Role.Code == RoleCodes.Owner))
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerMembership is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Propietario no encontrado",
                detail: "No se encontró un propietario activo para esta organización.");
        }

        var ownerUser = await dbContext.Users
            .SingleOrDefaultAsync(
                user => user.Id == ownerMembership.UserId,
                cancellationToken);

        if (ownerUser is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Usuario no encontrado",
                detail: "No se encontró el usuario asociado a la membresía.");
        }

        // Usar el teléfono proporcionado en la solicitud
        var phoneNumber = request.PhoneNumber;

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Teléfono requerido",
                detail: "Debe proporcionar un número de teléfono para enviar por WhatsApp.");
        }

        // Generar el mensaje de WhatsApp
        var loginUrl = $"{authOptions.Value.PublicUrl.TrimEnd('/')}/login";
        var message = $"*BIENVENIDO A LENDING PORTAL*%0A%0A"
            + $"Tu organizacion *{tenant.Name}* ha sido creada exitosamente.%0A%0A"
            + $"*Credenciales de acceso:*%0A"
            + $"Usuario: {ownerUser.Email.Value}%0A"
            + $"Contrasena: {request.Password}%0A%0A"
            + $"Ingresa aqui: {loginUrl}%0A%0A"
            + $"_Por seguridad, cambia tu contrasena despues del primer ingreso._";

        // Limpiar número de teléfono (solo dígitos)
        var cleanPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());
        
        // Generar URL de WhatsApp
        var whatsappUrl = $"https://wa.me/{cleanPhone}?text={message}";

        return TypedResults.Ok(new WhatsAppCredentialsResponse(
            whatsappUrl,
            ownerUser.Email.Value,
            phoneNumber,
            tenant.Name));
    }

    private static async Task<IResult> CreateTenantAsync(
        CreateTenantRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        TenantProvisioner provisioner,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IAuditWriter auditWriter,
        PlatformNotificationService notificationService,
        IClock clock,
        IOptions<AuthOptions> authOptions,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsPlatformAdmin)
        {
            return PlatformAdminRequired();
        }

        TenantSlug tenantSlug;
        Email ownerEmailValue;
        Tenant tenant;

        try
        {
            tenantSlug = TenantSlug.Create(request.Slug);
            ownerEmailValue = Email.Create(request.OwnerEmail);
            tenant = Tenant.Create(tenantSlug, request.Name, clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Datos inválidos",
                detail: exception.Message);
        }

        var slugExists = await dbContext.Tenants
            .AnyAsync(candidate => candidate.Slug == tenantSlug, cancellationToken);

        if (slugExists)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Identificador en uso",
                detail: "Ya existe una organización con ese identificador.");
        }

        // Verificar si el email ya existe
        var emailExists = await dbContext.Users
            .AnyAsync(u => u.Email == ownerEmailValue, cancellationToken);

        if (emailExists)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email en uso",
                detail: "Ya existe un usuario con ese correo electrónico en otra organización.");
        }

        // Normalizar el teléfono (agregar +51 si no tiene código de país)
        var normalizedPhone = NormalizePhoneNumber(request.OwnerPhone);

        // TODO: Implementar validación de teléfono único
        // Por ahora, omitimos la validación de teléfono duplicado
        // En el futuro, considerar agregar un campo Phone a la tabla Users o TenantSettings

        await SetSessionVariableAsync(dbContext, "app.tenant_id", tenant.Id, cancellationToken);

        // Crear el tenant
        dbContext.Tenants.Add(tenant);
        provisioner.AddDefaults(tenant);

        var now = clock.UtcNow;
        
        // Generar contraseña automática segura
        var generatedPassword = GenerateSecurePassword();
        var passwordHash = passwordHasher.Hash(generatedPassword);

        // Crear el usuario propietario
        var ownerUser = User.Create(
            ownerEmailValue,
            request.OwnerFullName,
            passwordHash,
            now,
            isPlatformAdmin: false);

        dbContext.Users.Add(ownerUser);

        // Obtener el rol de propietario
        var ownerRole = dbContext.Roles.Local
            .FirstOrDefault(role => role.TenantId == tenant.Id && role.Code == RoleCodes.Owner);

        // Crear la membresía
        if (ownerRole is not null)
        {
            dbContext.TenantMemberships.Add(TenantMembership.CreateActive(
                tenant.Id,
                ownerUser.Id,
                [ownerRole.Id],
                now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Enviar credenciales por email
        var loginUrl = $"{authOptions.Value.PublicUrl.TrimEnd('/')}/login";
        await emailSender.SendAsync(
            new EmailMessage(
                ownerEmailValue.Value,
                $"Bienvenido a {tenant.Name} - Credenciales de Acceso",
                $"Tu organización {tenant.Name} ha sido creada exitosamente.\n\n"
                + $"Credenciales de acceso:\n"
                + $"Email: {ownerEmailValue.Value}\n"
                + $"Contraseña: {generatedPassword}\n\n"
                + $"Ingresa aquí: {loginUrl}\n\n"
                + $"Por seguridad, te recomendamos cambiar tu contraseña después del primer ingreso."),
            cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                tenant.Id,
                currentUser.UserId,
                "platform.tenant.created",
                "Tenant",
                tenant.Id.ToString(),
                AfterJson: JsonSerializer.Serialize(new { 
                    tenant.Name, 
                    slug = tenantSlug.Value, 
                    owner = ownerEmailValue.Value,
                    ownerPhone = normalizedPhone,
                    hasLogo = !string.IsNullOrEmpty(tenant.LogoUrl)
                }),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        await notificationService.NotifyTenantCreatedAsync(
            tenant.Id,
            tenant.Name,
            cancellationToken);

        return TypedResults.Created(
            $"/api/v1/platform/tenants/{tenant.Id}",
            new CreateTenantResponse(
                tenant.Id,
                tenantSlug.Value,
                tenant.Name,
                tenant.Status.ToString(),
                ownerRole is null ? 0 : 1,
                tenant.CreatedAt,
                ownerEmailValue.Value,
                generatedPassword,
                normalizedPhone,
                tenant.LogoUrl));
    }
    
    private static string GenerateSecurePassword()
    {
        const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowercase = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "@#$%&*";
        
        var random = new Random();
        var password = new char[12];
        
        // Asegurar al menos un carácter de cada tipo
        password[0] = uppercase[random.Next(uppercase.Length)];
        password[1] = lowercase[random.Next(lowercase.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = special[random.Next(special.Length)];
        
        // Rellenar el resto
        var allChars = uppercase + lowercase + digits + special;
        for (int i = 4; i < password.Length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }
        
        // Mezclar
        return new string(password.OrderBy(_ => random.Next()).ToArray());
    }

    private static string NormalizePhoneNumber(string phone)
    {
        // Eliminar espacios, guiones y paréntesis
        var cleanPhone = new string(phone.Where(char.IsDigit).ToArray());
        
        // Si no empieza con código de país, agregar +51 (Perú)
        if (!phone.TrimStart().StartsWith("+"))
        {
            // Si tiene 9 dígitos, es un número peruano sin código
            if (cleanPhone.Length == 9)
            {
                return $"+51{cleanPhone}";
            }
            // Si tiene más de 9, agregar el + al inicio
            else if (cleanPhone.Length > 9)
            {
                return $"+{cleanPhone}";
            }
        }
        
        // Si ya tiene +, mantenerlo
        if (phone.TrimStart().StartsWith("+"))
        {
            return $"+{cleanPhone}";
        }
        
        // Por defecto, agregar +51
        return $"+51{cleanPhone}";
    }

    private static async Task<IResult> UpdateTenantStatusAsync(
        Guid id,
        UpdateTenantStatusRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IAuditWriter auditWriter,
        PlatformNotificationService notificationService,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsPlatformAdmin)
        {
            return PlatformAdminRequired();
        }

        var tenant = await dbContext.Tenants
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (tenant is null)
        {
            return TypedResults.NotFound();
        }

        var now = clock.UtcNow;
        if (request.Suspend)
        {
            tenant.Suspend(now);
        }
        else
        {
            tenant.Reactivate(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                tenant.Id,
                currentUser.UserId,
                request.Suspend ? "platform.tenant.suspended" : "platform.tenant.reactivated",
                "Tenant",
                tenant.Id.ToString(),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        if (request.Suspend)
        {
            await notificationService.NotifyTenantSuspendedAsync(
                tenant.Id,
                tenant.Name,
                cancellationToken);
        }
        else
        {
            await notificationService.NotifyTenantReactivatedAsync(
                tenant.Id,
                tenant.Name,
                cancellationToken);
        }

        return TypedResults.Ok(new PlatformTenantSummary(
            tenant.Id,
            tenant.Slug.Value,
            tenant.Name,
            tenant.Status.ToString(),
            0,
            tenant.CreatedAt));
    }

    private static Task<int> SetSessionVariableAsync(
        LendingDbContext dbContext,
        string variable,
        Guid value,
        CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config({variable}, {value.ToString()}, true)",
            cancellationToken);

    private static ProblemHttpResult PlatformAdminRequired() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Acceso de plataforma requerido",
            detail: "Esta operación requiere una cuenta de administrador de plataforma.");

    public sealed record PlatformTenantSummary(
        Guid Id,
        string Slug,
        string Name,
        string Status,
        int ActiveMembers,
        DateTimeOffset CreatedAt);

    public sealed record CreateTenantRequest(
        string Slug, 
        string Name, 
        string OwnerEmail, 
        string OwnerFullName,
        string OwnerPhone);
    
    public sealed record CreateTenantResponse(
        Guid Id,
        string Slug,
        string Name,
        string Status,
        int ActiveMembers,
        DateTimeOffset CreatedAt,
        string OwnerEmail,
        string GeneratedPassword,
        string OwnerPhone,
        string? LogoUrl);

    public sealed record UpdateTenantStatusRequest(bool Suspend);
    
    public sealed record PlatformDashboardResponse(
        int TotalTenants,
        int ActiveTenants,
        int SuspendedTenants,
        int TotalUsers,
        int ActiveUsers,
        int TotalMembers,
        IReadOnlyList<RecentTenantDto> RecentTenants);
    
    public sealed record RecentTenantDto(
        Guid Id,
        string Slug,
        string Name,
        string Status,
        DateTimeOffset CreatedAt);
    
    public sealed record WhatsAppCredentialsResponse(
        string WhatsAppUrl,
        string Email,
        string PhoneNumber,
        string TenantName);
    
    public sealed record SendCredentialsRequest(
        string PhoneNumber,
        string Password);

    private static async Task<IResult> GetTenantDetailsAsync(
        Guid id,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsPlatformAdmin)
        {
            return PlatformAdminRequired();
        }

        var tenant = await dbContext.Tenants
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (tenant is null)
        {
            return TypedResults.NotFound();
        }

        // Obtener el propietario
        var ownerMembership = await dbContext.TenantMemberships
            .Include(m => m.Roles)
            .ThenInclude(mr => mr.Role)
            .Where(m => m.TenantId == id 
                && m.Status == MembershipStatus.Active 
                && m.Roles.Any(r => r.Role.Code == RoleCodes.Owner))
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerMembership is null)
        {
            return TypedResults.NotFound();
        }

        var ownerUser = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Id == ownerMembership.UserId, cancellationToken);

        if (ownerUser is null)
        {
            return TypedResults.NotFound();
        }

        // Obtener teléfono del audit log
        var phoneNumber = await GetPhoneFromAuditLogAsync(dbContext, tenant.Id, cancellationToken);

        // Contar miembros activos
        var memberCount = await dbContext.TenantMemberships
            .CountAsync(m => m.TenantId == id && m.Status == MembershipStatus.Active, cancellationToken);

        return TypedResults.Ok(new TenantDetailsResponse(
            tenant.Id,
            tenant.Slug.Value,
            tenant.Name,
            tenant.Status.ToString(),
            memberCount,
            tenant.CreatedAt,
            ownerUser.Email.Value,
            ownerUser.FullName,
            phoneNumber ?? ""));
    }

    private static async Task<IResult> UpdateTenantAsync(
        Guid id,
        UpdateTenantRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsPlatformAdmin)
        {
            return PlatformAdminRequired();
        }

        var tenant = await dbContext.Tenants
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (tenant is null)
        {
            return TypedResults.NotFound();
        }

        // Actualizar nombre de la organización
        var beforeJson = JsonSerializer.Serialize(new { tenant.Name });

        try
        {
            // Usar reflexión para actualizar el nombre (ya que no hay método público)
            var nameProperty = typeof(Tenant).GetProperty("Name");
            nameProperty?.SetValue(tenant, request.Name);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Datos inválidos",
                detail: exception.Message);
        }

        // Actualizar información del propietario si se proporciona
        if (!string.IsNullOrWhiteSpace(request.OwnerFullName) || !string.IsNullOrWhiteSpace(request.OwnerPhone))
        {
            var ownerMembership = await dbContext.TenantMemberships
                .Where(m => m.TenantId == id 
                    && m.Status == MembershipStatus.Active 
                    && m.Roles.Any(r => r.Role.Code == RoleCodes.Owner))
                .FirstOrDefaultAsync(cancellationToken);

            if (ownerMembership is not null)
            {
                var ownerUser = await dbContext.Users
                    .SingleOrDefaultAsync(u => u.Id == ownerMembership.UserId, cancellationToken);

                if (ownerUser is not null && !string.IsNullOrWhiteSpace(request.OwnerFullName))
                {
                    var fullNameProperty = typeof(User).GetProperty("FullName");
                    fullNameProperty?.SetValue(ownerUser, request.OwnerFullName);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var normalizedPhone = !string.IsNullOrWhiteSpace(request.OwnerPhone) 
            ? NormalizePhoneNumber(request.OwnerPhone) 
            : null;

        await auditWriter.RecordAsync(
            new AuditEntry(
                tenant.Id,
                currentUser.UserId,
                "platform.tenant.updated",
                "Tenant",
                tenant.Id.ToString(),
                BeforeJson: beforeJson,
                AfterJson: JsonSerializer.Serialize(new { 
                    tenant.Name,
                    ownerFullName = request.OwnerFullName,
                    ownerPhone = normalizedPhone
                }),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        return TypedResults.Ok(new TenantDetailsResponse(
            tenant.Id,
            tenant.Slug.Value,
            tenant.Name,
            tenant.Status.ToString(),
            0,
            tenant.CreatedAt,
            request.OwnerEmail ?? "",
            request.OwnerFullName ?? "",
            normalizedPhone ?? ""));
    }

    private static async Task<IResult> ResetOwnerPasswordAsync(
        Guid id,
        ResetPasswordRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IAuditWriter auditWriter,
        IPasswordHasher passwordHasher,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsPlatformAdmin)
        {
            return PlatformAdminRequired();
        }

        var tenant = await dbContext.Tenants
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (tenant is null)
        {
            return TypedResults.NotFound();
        }

        var ownerMembership = await dbContext.TenantMemberships
            .Where(m => m.TenantId == id 
                && m.Status == MembershipStatus.Active 
                && m.Roles.Any(r => r.Role.Code == RoleCodes.Owner))
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerMembership is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Propietario no encontrado",
                detail: "No se encontró un propietario activo para esta organización.");
        }

        var ownerUser = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Id == ownerMembership.UserId, cancellationToken);

        if (ownerUser is null)
        {
            return TypedResults.NotFound();
        }

        // Generar nueva contraseña o usar la proporcionada
        var newPassword = string.IsNullOrWhiteSpace(request.NewPassword) 
            ? GenerateSecurePassword() 
            : request.NewPassword;

        var passwordHash = passwordHasher.Hash(newPassword);

        // Actualizar contraseña
        var passwordProperty = typeof(User).GetProperty("PasswordHash");
        passwordProperty?.SetValue(ownerUser, passwordHash);

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                tenant.Id,
                currentUser.UserId,
                "platform.tenant.owner.password_reset",
                "User",
                ownerUser.Id.ToString(),
                CorrelationId: httpContext.TraceIdentifier,
                IpHash: IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString())),
            cancellationToken);

        return TypedResults.Ok(new ResetPasswordResponse(
            ownerUser.Email.Value,
            newPassword));
    }

    private static async Task<string?> GetPhoneFromAuditLogAsync(
        LendingDbContext dbContext,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var auditEntry = await dbContext.AuditEvents
            .Where(a => a.EntityId == tenantId.ToString() && a.Action == "platform.tenant.created")
            .OrderByDescending(a => a.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (auditEntry?.AfterJson is null)
        {
            return null;
        }

        try
        {
            var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditEntry.AfterJson);
            if (json != null && json.TryGetValue("ownerPhone", out var phoneElement))
            {
                return phoneElement.GetString();
            }
        }
        catch
        {
            // Ignorar errores de parsing
        }

        return null;
    }

    public sealed record TenantDetailsResponse(
        Guid Id,
        string Slug,
        string Name,
        string Status,
        int MemberCount,
        DateTimeOffset CreatedAt,
        string OwnerEmail,
        string OwnerFullName,
        string OwnerPhone);

    public sealed record UpdateTenantRequest(
        string Name,
        string? OwnerEmail,
        string? OwnerFullName,
        string? OwnerPhone);

    public sealed record ResetPasswordRequest(string? NewPassword);

    public sealed record ResetPasswordResponse(
        string Email,
        string NewPassword);
}
