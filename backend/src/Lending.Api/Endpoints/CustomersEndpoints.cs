using Lending.Api.Authentication;
using Lending.Api.Authorization;
using Lending.Application.Abstractions;
using Lending.Application.Common;
using Lending.Domain.Common;
using Lending.Domain.Customers;
using Lending.Domain.Identity;
using Lending.Infrastructure.Persistence;
using Lending.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lending.Api.Endpoints;

public static class CustomersEndpoints
{
    private const string DocumentNumberUniqueIndex = "IX_customers_TenantId_DocumentNumber";
    private const string DuplicateDocumentMessage =
        "Este número de documento ya está registrado en tu empresa";

    public static IEndpointRouteBuilder MapCustomersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers")
            .WithTags("Customers")
            .RequireAuthorization();

        group.MapGet("/", ListCustomersAsync)
            .WithName("ListCustomers")
            .RequirePermission(Permissions.CustomersView);

        group.MapGet("/{id:guid}", GetCustomerAsync)
            .WithName("GetCustomer")
            .RequirePermission(Permissions.CustomersView);

        group.MapPost("/", CreateCustomerAsync)
            .WithName("CreateCustomer")
            .RequirePermission(Permissions.CustomersManage);

        group.MapPut("/{id:guid}", UpdateCustomerAsync)
            .WithName("UpdateCustomer")
            .RequirePermission(Permissions.CustomersManage);

        group.MapPatch("/{id:guid}/status", UpdateCustomerStatusAsync)
            .WithName("UpdateCustomerStatus")
            .RequirePermission(Permissions.CustomersManage);

        group.MapDelete("/{id:guid}", DeleteCustomerAsync)
            .WithName("DeleteCustomer")
            .RequirePermission(Permissions.CustomersManage);

        group.MapPost("/{id:guid}/references", AddReferenceAsync)
            .WithName("AddCustomerReference")
            .RequirePermission(Permissions.CustomersManage);

        group.MapDelete("/{id:guid}/references/{referenceId:guid}", RemoveReferenceAsync)
            .WithName("RemoveCustomerReference")
            .RequirePermission(Permissions.CustomersManage);

        group.MapPost("/{id:guid}/photo", UploadCustomerPhotoAsync)
            .WithName("UploadCustomerPhoto")
            .DisableAntiforgery()
            .RequirePermission(Permissions.CustomersManage);

        group.MapGet("/check-document", CheckDocumentAvailabilityAsync)
            .WithName("CheckDocumentAvailability")
            .RequirePermission(Permissions.CustomersView);

        return endpoints;
    }

    private static async Task<IResult> ListCustomersAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        string? search,
        string? status,
        string? sortBy,
        string? sortDirection,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var query = dbContext.Customers
            .AsNoTracking()
            .Where(c => c.TenantId == currentUser.TenantId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.FullName, pattern) ||
                EF.Functions.ILike(c.DocumentNumber, pattern) ||
                (c.Phone != null && EF.Functions.ILike(c.Phone, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CustomerStatus>(status, true, out var statusEnum))
        {
            query = query.Where(c => c.Status == statusEnum);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var ascending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        var ordered = sortBy?.Trim().ToLowerInvariant() switch
        {
            "createdat" => ascending
                ? query.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
                : query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id),
            "document" => ascending
                ? query.OrderBy(c => c.DocumentNumber).ThenBy(c => c.Id)
                : query.OrderByDescending(c => c.DocumentNumber).ThenByDescending(c => c.Id),
            _ => ascending
                ? query.OrderBy(c => c.FullName).ThenBy(c => c.Id)
                : query.OrderByDescending(c => c.FullName).ThenByDescending(c => c.Id),
        };

        var rows = await ordered
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(c => new
            {
                c.Id,
                c.Type,
                c.DocumentType,
                c.DocumentNumber,
                c.FullName,
                c.Phone,
                c.PhotoUrl,
                c.Status,
                c.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(c => new CustomerSummary(
                c.Id,
                c.Type.ToString(),
                c.DocumentType,
                c.DocumentNumber,
                c.FullName,
                c.Phone,
                c.PhotoUrl,
                c.Status.ToString(),
                c.CreatedAt))
            .ToArray();

        return TypedResults.Ok(new PagedResult<CustomerSummary>(
            items,
            page.PageNumber,
            page.PageSize,
            totalCount));
    }

    private static async Task<IResult> GetCustomerAsync(
        Guid id,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var customer = await dbContext.Customers
            .AsNoTracking()
            .Include(c => c.References)
            .SingleOrDefaultAsync(c => c.Id == id && c.TenantId == currentUser.TenantId.Value, cancellationToken);

        if (customer is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new CustomerDetails(
            customer.Id,
            customer.Type.ToString(),
            customer.DocumentType,
            customer.DocumentNumber,
            customer.FullName,
            customer.BirthDate,
            customer.Phone,
            customer.Email,
            customer.PhotoUrl,
            customer.Address,
            customer.District,
            customer.Province,
            customer.Department,
            customer.Occupation,
            customer.Employer,
            customer.MonthlyIncome,
            customer.Status.ToString(),
            customer.Notes,
            customer.References.Select(r => new CustomerReferenceDetails(
                r.Id,
                r.FullName,
                r.Relationship,
                r.Phone)).ToArray(),
            customer.CreatedAt,
            customer.UpdatedAt));
    }

    private static async Task<IResult> CreateCustomerAsync(
        CreateCustomerRequest request,
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

        var tenantId = currentUser.TenantId.Value;
        var now = clock.UtcNow;
        var currentDate = await GetTenantTodayAsync(dbContext, tenantId, clock, cancellationToken);

        Customer customer;
        try
        {
            customer = Customer.Create(
                tenantId,
                Enum.Parse<CustomerType>(request.Type, true),
                request.DocumentType,
                request.DocumentNumber,
                request.FullName,
                now,
                currentDate,
                request.BirthDate);

            customer.Update(
                request.FullName,
                request.BirthDate,
                request.Phone,
                request.Email,
                request.Address,
                request.District,
                request.Province,
                request.Department,
                request.Occupation,
                request.Employer,
                request.MonthlyIncome,
                now,
                currentDate);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Datos inválidos",
                detail: exception.Message);
        }

        var documentNumberExists = await dbContext.Customers
            .AsNoTracking()
            .AnyAsync(
                c => c.TenantId == tenantId && c.DocumentNumber == customer.DocumentNumber,
                cancellationToken);

        if (documentNumberExists)
        {
            return DuplicateDocumentConflict();
        }

        dbContext.Customers.Add(customer);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateDocumentNumberViolation(exception))
        {
            return DuplicateDocumentConflict();
        }

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "customer.created",
                "Customer",
                customer.Id.ToString()),
            cancellationToken);

        return TypedResults.Created($"/api/v1/customers/{customer.Id}", new { id = customer.Id });
    }

    private static async Task<IResult> UpdateCustomerAsync(
        Guid id,
        UpdateCustomerRequest request,
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

        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(c => c.Id == id && c.TenantId == currentUser.TenantId.Value, cancellationToken);

        if (customer is null)
        {
            return TypedResults.NotFound();
        }

        var currentDate = await GetTenantTodayAsync(
            dbContext,
            currentUser.TenantId.Value,
            clock,
            cancellationToken);

        try
        {
            customer.Update(
                request.FullName,
                request.BirthDate,
                request.Phone,
                request.Email,
                request.Address,
                request.District,
                request.Province,
                request.Department,
                request.Occupation,
                request.Employer,
                request.MonthlyIncome,
                clock.UtcNow,
                currentDate);
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
                currentUser.TenantId,
                currentUser.UserId,
                "customer.updated",
                "Customer",
                customer.Id.ToString()),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> UpdateCustomerStatusAsync(
        Guid id,
        UpdateCustomerStatusRequest request,
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

        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(c => c.Id == id && c.TenantId == currentUser.TenantId.Value, cancellationToken);

        if (customer is null)
        {
            return TypedResults.NotFound();
        }

        if (request.Status == "Active")
        {
            customer.Activate(clock.UtcNow);
        }
        else if (request.Status == "Suspended")
        {
            customer.Suspend(clock.UtcNow);
        }
        else if (request.Status == "Blocked")
        {
            customer.Block(clock.UtcNow);
        }
        else
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Estado inválido",
                detail: "Use: Active, Suspended o Blocked");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "customer.status.updated",
                "Customer",
                customer.Id.ToString()),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> DeleteCustomerAsync(
        Guid id,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IAuditWriter auditWriter,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var tenantId = currentUser.TenantId.Value;
        var customer = await dbContext.Customers
            .Include(c => c.References)
            .SingleOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, cancellationToken);

        if (customer is null)
        {
            return TypedResults.NotFound();
        }

        var hasLoans = await dbContext.Loans
            .AnyAsync(l => l.TenantId == tenantId && l.CustomerId == id, cancellationToken);

        if (hasLoans)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Cliente con préstamos",
                detail: "No se puede eliminar un cliente que tiene préstamos asociados. Suspéndelo o bloquéalo en su lugar.");
        }

        dbContext.Customers.Remove(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                tenantId,
                currentUser.UserId,
                "customer.deleted",
                "Customer",
                id.ToString()),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> AddReferenceAsync(
        Guid id,
        AddCustomerReferenceRequest request,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var customer = await dbContext.Customers
            .Include(c => c.References)
            .SingleOrDefaultAsync(c => c.Id == id && c.TenantId == currentUser.TenantId.Value, cancellationToken);

        if (customer is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            customer.AddReference(request.FullName, request.Relationship, request.Phone, clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Datos inválidos",
                detail: exception.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> RemoveReferenceAsync(
        Guid id,
        Guid referenceId,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var customer = await dbContext.Customers
            .Include(c => c.References)
            .SingleOrDefaultAsync(c => c.Id == id && c.TenantId == currentUser.TenantId.Value, cancellationToken);

        if (customer is null)
        {
            return TypedResults.NotFound();
        }

        customer.RemoveReference(referenceId, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> CheckDocumentAvailabilityAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        string documentType,
        string documentNumber,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        string normalizedDocumentType;
        string normalizedDocumentNumber;
        try
        {
            normalizedDocumentType = Customer.NormalizeDocumentType(documentType);
            normalizedDocumentNumber = Customer.NormalizeDocumentNumber(
                normalizedDocumentType,
                documentNumber);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Documento inválido",
                detail: exception.Message);
        }

        var exists = await dbContext.Customers
            .AsNoTracking()
            .AnyAsync(
                c => c.TenantId == currentUser.TenantId.Value &&
                     c.DocumentType == normalizedDocumentType &&
                     c.DocumentNumber == normalizedDocumentNumber,
                cancellationToken);

        if (exists)
        {
            return TypedResults.Ok(new
            {
                available = false,
                message = DuplicateDocumentMessage
            });
        }

        return TypedResults.Ok(new
        {
            available = true,
            message = "Documento disponible para registrar"
        });
    }

    private static async Task<DateOnly> GetTenantTodayAsync(
        LendingDbContext dbContext,
        Guid tenantId,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var timeZoneId = await dbContext.TenantSettings
            .AsNoTracking()
            .Where(settings => settings.TenantId == tenantId)
            .Select(settings => settings.Timezone)
            .SingleOrDefaultAsync(cancellationToken);

        return clock.Today(string.IsNullOrWhiteSpace(timeZoneId) ? "America/Lima" : timeZoneId);
    }

    private static ProblemHttpResult TenantRequired() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Organización requerida",
            detail: "Selecciona una organización antes de continuar.");

    private static ProblemHttpResult DuplicateDocumentConflict() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Documento ya registrado",
            detail: DuplicateDocumentMessage);

    private static bool IsDuplicateDocumentNumberViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: DocumentNumberUniqueIndex,
        };

    public sealed record CustomerSummary(
        Guid Id,
        string Type,
        string DocumentType,
        string DocumentNumber,
        string FullName,
        string? Phone,
        string? PhotoUrl,
        string Status,
        DateTimeOffset CreatedAt);

    public sealed record CustomerDetails(
        Guid Id,
        string Type,
        string DocumentType,
        string DocumentNumber,
        string FullName,
        DateOnly? BirthDate,
        string? Phone,
        string? Email,
        string? PhotoUrl,
        string? Address,
        string? District,
        string? Province,
        string? Department,
        string? Occupation,
        string? Employer,
        decimal? MonthlyIncome,
        string Status,
        string? Notes,
        IReadOnlyList<CustomerReferenceDetails> References,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed record CustomerReferenceDetails(
        Guid Id,
        string FullName,
        string? Relationship,
        string Phone);

    public sealed record CreateCustomerRequest(
        string Type,
        string DocumentType,
        string DocumentNumber,
        string FullName,
        DateOnly? BirthDate,
        string? Phone,
        string? Email,
        string? Address,
        string? District,
        string? Province,
        string? Department,
        string? Occupation,
        string? Employer,
        decimal? MonthlyIncome);

    public sealed record UpdateCustomerRequest(
        string FullName,
        DateOnly? BirthDate,
        string? Phone,
        string? Email,
        string? Address,
        string? District,
        string? Province,
        string? Department,
        string? Occupation,
        string? Employer,
        decimal? MonthlyIncome);

    public sealed record UpdateCustomerStatusRequest(string Status);

    public sealed record AddCustomerReferenceRequest(
        string FullName,
        string? Relationship,
        string Phone);

    private static async Task<IResult> UploadCustomerPhotoAsync(
        Guid id,
        HttpContext httpContext,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IFileUploadService fileUploadService,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var tenantId = currentUser.TenantId.Value;
        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, cancellationToken);

        if (customer is null)
        {
            return TypedResults.NotFound();
        }

        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var photoFile = form.Files.GetFile("photo");

        if (photoFile is null || photoFile.Length == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Archivo requerido",
                detail: "Debe proporcionar una foto del cliente.");
        }

        try
        {
            using var stream = photoFile.OpenReadStream();
            var photoUrl = await fileUploadService.UploadImageAsync(
                stream,
                photoFile.FileName,
                $"customers/{tenantId}",
                cancellationToken);

            customer.SetPhoto(photoUrl, clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);

            return TypedResults.Ok(new { photoUrl });
        }
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    throw;
}
catch (Exception)
{
    return TypedResults.Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "Error al subir foto",
        detail: "No fue posible procesar la imagen. Inténtalo nuevamente.");
}
    }
}