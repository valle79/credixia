using Lending.Api.Authentication;
using Lending.Api.Authorization;
using Lending.Application.Abstractions;
using Lending.Application.Common;
using Lending.Domain.Collections;
using Lending.Domain.Common;
using Lending.Domain.Identity;
using Lending.Domain.Loans;
using Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lending.Api.Endpoints;

public static class CollectionsEndpoints
{
    public static IEndpointRouteBuilder MapCollectionsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/collections")
            .WithTags("Collections")
            .RequireAuthorization();

        group.MapGet("/overdue", ListOverdueLoansAsync)
            .WithName("ListOverdueLoans")
            .RequirePermission(Permissions.CollectionsManage);

        group.MapGet("/actions", ListCollectionActionsAsync)
            .WithName("ListCollectionActions")
            .RequirePermission(Permissions.CollectionsManage);

        group.MapPost("/actions", RecordCollectionActionAsync)
            .WithName("RecordCollectionAction")
            .RequirePermission(Permissions.CollectionsManage);

        return endpoints;
    }

    private static async Task<IResult> ListOverdueLoansAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IClock clock,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var tenantId = currentUser.TenantId.Value;

        var query = dbContext.Loans
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId &&
                (l.Status == LoanStatus.Active || l.Status == LoanStatus.DefaultedDef) &&
                l.Schedule.Any(item =>
                    item.DueDate < today && item.PaidAmount.Amount < item.DueAmount.Amount));

        var totalCount = await query.CountAsync(cancellationToken);
        var totalOverdueAmount = await query
            .SelectMany(l => l.Schedule)
            .Where(item => item.DueDate < today && item.PaidAmount.Amount < item.DueAmount.Amount)
            .SumAsync(
                item => (decimal?)(item.DueAmount.Amount + item.LateFee.Amount - item.PaidAmount.Amount),
                cancellationToken) ?? 0m;

        var page = new PageRequest(pageNumber, pageSize, 50, 200);
        var rows = await query
            .OrderBy(l => l.Schedule
                .Where(item => item.DueDate < today && item.PaidAmount.Amount < item.DueAmount.Amount)
                .Min(item => item.DueDate))
            .ThenBy(l => l.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(l => new
            {
                l.Id,
                l.LoanNumber,
                l.CustomerId,
                CustomerName = dbContext.Customers
                    .Where(c => c.Id == l.CustomerId)
                    .Select(c => c.FullName)
                    .FirstOrDefault() ?? "Desconocido",
                CustomerPhone = dbContext.Customers
                    .Where(c => c.Id == l.CustomerId)
                    .Select(c => c.Phone)
                    .FirstOrDefault(),
                l.LoanProductId,
                ProductName = dbContext.LoanProducts
                    .Where(p => p.Id == l.LoanProductId)
                    .Select(p => p.Name)
                    .FirstOrDefault(),
                l.PrincipalAmount,
                l.TotalAmount,
                l.BalanceAmount,
                l.InterestRate,
                l.Status,
                l.DisbursementDate,
                l.CreatedAt,
                NextPaymentDueDate = l.Schedule
                    .Where(item => item.DueDate < today && item.PaidAmount.Amount < item.DueAmount.Amount)
                    .Min(item => item.DueDate),
                TotalDue = l.Schedule
                    .Where(item => item.DueDate < today && item.PaidAmount.Amount < item.DueAmount.Amount)
                    .Sum(item => item.DueAmount.Amount + item.LateFee.Amount - item.PaidAmount.Amount),
            })
            .ToListAsync(cancellationToken);

        var loanIds = rows.Select(row => row.Id).ToArray();
        var lastActions = await dbContext.CollectionActions
            .AsNoTracking()
            .Where(action => loanIds.Contains(action.LoanId))
            .GroupBy(action => action.LoanId)
            .Select(group => new
            {
                LoanId = group.Key,
                LastRegisteredAt = group.Max(action => action.RegisteredAt),
            })
            .ToDictionaryAsync(item => item.LoanId, item => item.LastRegisteredAt, cancellationToken);

        var items = rows
            .Select(row => new OverdueLoanDto(
                row.Id,
                row.LoanNumber,
                row.CustomerId,
                row.CustomerName,
                row.CustomerPhone,
                row.LoanProductId,
                row.ProductName,
                row.PrincipalAmount.Amount,
                row.TotalAmount.Amount,
                row.BalanceAmount.Amount,
                row.PrincipalAmount.Currency.Code,
                row.InterestRate,
                row.Status.ToString(),
                row.DisbursementDate,
                row.CreatedAt,
                row.NextPaymentDueDate,
                row.TotalDue,
                lastActions.GetValueOrDefault(row.Id)))
            .ToArray();

        return TypedResults.Ok(new OverdueLoansResponse(
            items,
            page.PageNumber,
            page.PageSize,
            totalCount,
            totalOverdueAmount));
    }

    private static async Task<IResult> ListCollectionActionsAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        Guid? loanId,
        string? actionType,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var tenantLoanIds = dbContext.Loans
            .AsNoTracking()
            .Where(l => l.TenantId == currentUser.TenantId.Value)
            .Select(l => l.Id);

        var query = dbContext.CollectionActions
            .AsNoTracking()
            .Where(a => tenantLoanIds.Contains(a.LoanId));

        if (loanId.HasValue)
        {
            query = query.Where(a => a.LoanId == loanId.Value);
        }

        if (!string.IsNullOrWhiteSpace(actionType) &&
            Enum.TryParse<CollectionActionType>(actionType, true, out var type))
        {
            query = query.Where(a => a.Type == type);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize, 50, 200);

        var actions = await query
            .OrderByDescending(a => a.RegisteredAt)
            .ThenByDescending(a => a.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        var items = await BuildActionDtosAsync(dbContext, actions, cancellationToken);

        return TypedResults.Ok(new PagedResult<CollectionActionDto>(
            items,
            page.PageNumber,
            page.PageSize,
            totalCount));
    }

    private static async Task<IResult> RecordCollectionActionAsync(
        RecordCollectionActionRequest request,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IAuditWriter auditWriter,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue || !currentUser.UserId.HasValue)
        {
            return TenantRequired();
        }

        var loan = await dbContext.Loans
            .AsNoTracking()
            .SingleOrDefaultAsync(
                l => l.Id == request.LoanId && l.TenantId == currentUser.TenantId.Value,
                cancellationToken);

        if (loan is null)
        {
            return TypedResults.NotFound();
        }

        if (!Enum.TryParse<CollectionActionType>(request.ActionType, true, out var actionType))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Tipo de acción inválido",
                detail: "Use: PhoneCall, Visit, Message, Promise o LegalAction.");
        }

        DateOnly? promiseDate = null;
        if (!string.IsNullOrWhiteSpace(request.FollowUpDate))
        {
            if (DateOnly.TryParse(request.FollowUpDate, out var parsedDate))
            {
                promiseDate = parsedDate;
            }
            else if (DateTimeOffset.TryParse(request.FollowUpDate, out var parsedDateTime))
            {
                promiseDate = DateOnly.FromDateTime(parsedDateTime.UtcDateTime);
            }
            else
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Fecha inválida",
                    detail: "La fecha de seguimiento no tiene un formato válido.");
            }
        }

        CollectionAction action;
        try
        {
            action = CollectionAction.Create(
                loan.Id,
                currentUser.UserId.Value,
                actionType,
                string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                promiseDate,
                clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Datos inválidos",
                detail: exception.Message);
        }

        dbContext.CollectionActions.Add(action);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "collection.action.recorded",
                "CollectionAction",
                action.Id.ToString()),
            cancellationToken);

        var dto = (await BuildActionDtosAsync(dbContext, [action], cancellationToken)).Single();
        return TypedResults.Created($"/api/v1/collections/actions/{action.Id}", dto);
    }

    private static async Task<CollectionActionDto[]> BuildActionDtosAsync(
        LendingDbContext dbContext,
        IReadOnlyCollection<CollectionAction> actions,
        CancellationToken cancellationToken)
    {
        var loanIds = actions.Select(a => a.LoanId).Distinct().ToArray();
        var loans = await dbContext.Loans
            .AsNoTracking()
            .Where(l => loanIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var customerIds = loans.Values.Select(l => l.CustomerId).Distinct().ToArray();
        var customers = await dbContext.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.FullName, cancellationToken);

        var userIds = actions.Select(a => a.CollectorUserId).Distinct().ToArray();
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        return actions
            .Select(action =>
            {
                var loan = loans.GetValueOrDefault(action.LoanId);
                var customerName = loan is not null
                    ? customers.GetValueOrDefault(loan.CustomerId) ?? "Desconocido"
                    : "Desconocido";

                return new CollectionActionDto(
                    action.Id,
                    action.LoanId,
                    loan?.LoanNumber ?? "Desconocido",
                    customerName,
                    action.Type.ToString(),
                    action.RegisteredAt,
                    action.Notes,
                    action.PromiseDate,
                    users.GetValueOrDefault(action.CollectorUserId, "Desconocido"),
                    action.RegisteredAt);
            })
            .ToArray();
    }

    private static ProblemHttpResult TenantRequired() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Organización requerida",
            detail: "Selecciona una organización antes de continuar.");

    public sealed record RecordCollectionActionRequest(
        Guid LoanId,
        string ActionType,
        string? Notes,
        string? FollowUpDate);

    public sealed record OverdueLoanDto(
        Guid Id,
        string LoanNumber,
        Guid CustomerId,
        string CustomerName,
        string? CustomerPhone,
        Guid LoanProductId,
        string? ProductName,
        decimal PrincipalAmount,
        decimal TotalAmount,
        decimal BalanceAmount,
        string Currency,
        decimal InterestRate,
        string Status,
        DateOnly DisbursementDate,
        DateTimeOffset CreatedAt,
        DateOnly NextPaymentDueDate,
        decimal TotalDue,
        DateTimeOffset? LastCollectionDate);

    public sealed record CollectionActionDto(
        Guid Id,
        Guid LoanId,
        string LoanNumber,
        string CustomerName,
        string ActionType,
        DateTimeOffset ActionDate,
        string? Notes,
        DateOnly? FollowUpDate,
        string PerformedBy,
        DateTimeOffset CreatedAt);

    public sealed record OverdueLoansResponse(
        IReadOnlyList<OverdueLoanDto> Items,
        int PageNumber,
        int PageSize,
        int TotalCount,
        decimal TotalOverdueAmount)
    {
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
