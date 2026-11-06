using Lending.Api.Authentication;
using Lending.Api.Authorization;
using Lending.Application.Abstractions;
using Lending.Application.Common;
using Lending.Domain.Common;
using Lending.Domain.Identity;
using Lending.Domain.LoanProducts;
using Lending.Domain.Loans;
using Lending.Domain.ValueObjects;
using Lending.Infrastructure.Documents;
using Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Lending.Domain.Tenancy;

namespace Lending.Api.Endpoints;

public static class LoansEndpoints
{
    public static IEndpointRouteBuilder MapLoansEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/loans")
            .WithTags("Loans")
            .RequireAuthorization();

        group.MapGet("/", ListLoansAsync)
            .WithName("ListLoans")
            .RequirePermission(Permissions.LoansView);

        group.MapGet("/{id:guid}", GetLoanAsync)
            .WithName("GetLoan")
            .RequirePermission(Permissions.LoansView);

        group.MapDelete("/{id:guid}", DeleteLoanAsync)
            .WithName("DeleteLoan")
            .RequirePermission(Permissions.LoansManage);

        group.MapPost("/", CreateLoanAsync)
            .WithName("CreateLoan")
            .RequirePermission(Permissions.LoansManage);

        group.MapPost("/{id:guid}/approve", ApproveLoanAsync)
            .WithName("ApproveLoan")
            .RequirePermission(Permissions.LoansManage);

        group.MapPost("/{id:guid}/reject", RejectLoanAsync)
            .WithName("RejectLoan")
            .RequirePermission(Permissions.LoansManage);

        group.MapPost("/{id:guid}/disburse", DisburseLoanAsync)
            .WithName("DisburseLoan")
            .RequirePermission(Permissions.CashManage);

        group.MapPost("/{id:guid}/payments", RegisterPaymentAsync)
            .WithName("RegisterPayment")
            .RequirePermission(Permissions.PaymentsManage);

        group.MapGet("/{id:guid}/schedule", GetLoanScheduleAsync)
            .WithName("GetLoanSchedule")
            .RequirePermission(Permissions.LoansView);

        group.MapGet("/{id:guid}/schedule/pdf", DownloadLoanSchedulePdfAsync)
            .WithName("DownloadLoanSchedulePdf")
            .RequirePermission(Permissions.LoansView);

        group.MapPost("/{id:guid}/settlement/calculate", CalculateSettlementAsync)
            .WithName("CalculateSettlement")
            .RequirePermission(Permissions.LoansView);

        group.MapPost("/{id:guid}/settlement", SettleLoanAsync)
            .WithName("SettleLoan")
            .RequirePermission(Permissions.LoansManage);

        group.MapPost("/{id:guid}/refinance/simulate", SimulateRefinancingAsync)
            .WithName("SimulateRefinancing")
            .RequirePermission(Permissions.LoansView);

        group.MapPost("/{id:guid}/refinance", RefinanceLoanAsync)
            .WithName("RefinanceLoan")
            .RequirePermission(Permissions.LoansManage);

        return endpoints;
    }

    private static async Task<IResult> ListLoansAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IClock clock,
        Guid? customerId,
        Guid? productId,
        string? status,
        string? search,
        DateOnly? fromDate,
        DateOnly? toDate,
        bool? overdueOnly,
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

        var tenantId = currentUser.TenantId.Value;
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var query = dbContext.Loans
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId);

        if (customerId.HasValue)
        {
            query = query.Where(l => l.CustomerId == customerId.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(l => l.LoanProductId == productId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LoanStatus>(status, true, out var statusEnum))
        {
            query = query.Where(l => l.Status == statusEnum);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.DisbursementDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(l => l.DisbursementDate <= toDate.Value);
        }

        if (overdueOnly == true)
        {
            query = query.Where(l =>
                (l.Status == LoanStatus.Active || l.Status == LoanStatus.DefaultedDef) &&
                l.Schedule.Any(item =>
                    item.DueDate < today && item.PaidAmount.Amount < item.DueAmount.Amount));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(l =>
                EF.Functions.ILike(l.LoanNumber, pattern) ||
                dbContext.Customers.Any(c =>
                    c.Id == l.CustomerId &&
                    (EF.Functions.ILike(c.FullName, pattern) ||
                     EF.Functions.ILike(c.DocumentNumber, pattern))));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var ordered = ApplyLoanOrdering(query, dbContext, sortBy, sortDirection);

        var rows = await ordered
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
                CustomerPhotoUrl = dbContext.Customers
                    .Where(c => c.Id == l.CustomerId)
                    .Select(c => c.PhotoUrl)
                    .FirstOrDefault(),
                l.PrincipalAmount,
                l.TotalAmount,
                l.BalanceAmount,
                l.InterestRate,
                l.Status,
                l.DisbursementDate,
                l.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(l => new LoanSummary(
                l.Id,
                l.LoanNumber,
                l.CustomerId,
                l.CustomerName,
                l.CustomerPhotoUrl,
                l.PrincipalAmount.Amount,
                l.TotalAmount.Amount,
                l.BalanceAmount.Amount,
                l.PrincipalAmount.Currency.Code,
                l.InterestRate,
                l.Status.ToString(),
                l.DisbursementDate,
                l.CreatedAt))
            .ToArray();

        return TypedResults.Ok(new PagedResult<LoanSummary>(
            items,
            page.PageNumber,
            page.PageSize,
            totalCount));
    }

    private static IOrderedQueryable<Loan> ApplyLoanOrdering(
        IQueryable<Loan> query,
        LendingDbContext dbContext,
        string? sortBy,
        string? sortDirection)
    {
        var ascending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "amount" or "principal" => ascending
                ? query.OrderBy(l => l.PrincipalAmount.Amount).ThenBy(l => l.Id)
                : query.OrderByDescending(l => l.PrincipalAmount.Amount).ThenByDescending(l => l.Id),
            "balance" => ascending
                ? query.OrderBy(l => l.BalanceAmount.Amount).ThenBy(l => l.Id)
                : query.OrderByDescending(l => l.BalanceAmount.Amount).ThenByDescending(l => l.Id),
            "status" => ascending
                ? query.OrderBy(l => l.Status).ThenBy(l => l.Id)
                : query.OrderByDescending(l => l.Status).ThenByDescending(l => l.Id),
            "loannumber" => ascending
                ? query.OrderBy(l => l.LoanNumber).ThenBy(l => l.Id)
                : query.OrderByDescending(l => l.LoanNumber).ThenByDescending(l => l.Id),
            "disbursementdate" => ascending
                ? query.OrderBy(l => l.DisbursementDate).ThenBy(l => l.Id)
                : query.OrderByDescending(l => l.DisbursementDate).ThenByDescending(l => l.Id),
            "customer" => ascending
                ? query
                    .OrderBy(l => dbContext.Customers
                        .Where(c => c.Id == l.CustomerId)
                        .Select(c => c.FullName)
                        .FirstOrDefault())
                    .ThenBy(l => l.Id)
                : query
                    .OrderByDescending(l => dbContext.Customers
                        .Where(c => c.Id == l.CustomerId)
                        .Select(c => c.FullName)
                        .FirstOrDefault())
                    .ThenByDescending(l => l.Id),
            _ => ascending
                ? query.OrderBy(l => l.CreatedAt).ThenBy(l => l.Id)
                : query.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id),
        };
    }

    private static async Task<IResult> GetLoanAsync(
        Guid id,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var loan = await dbContext.Loans
            .AsNoTracking()
            .Where(l => l.Id == id && l.TenantId == currentUser.TenantId.Value)
            .Select(l => new
            {
                l.Id,
                l.LoanNumber,
                l.CustomerId,
                CustomerName = dbContext.Customers
                    .Where(c => c.Id == l.CustomerId)
                    .Select(c => c.FullName)
                    .FirstOrDefault() ?? "Desconocido",
                CustomerPhotoUrl = dbContext.Customers
                    .Where(c => c.Id == l.CustomerId)
                    .Select(c => c.PhotoUrl)
                    .FirstOrDefault(),
                CustomerPhone = dbContext.Customers
                    .Where(c => c.Id == l.CustomerId)
                    .Select(c => c.Phone)
                    .FirstOrDefault(),
                l.LoanProductId,
                ProductName = dbContext.LoanProducts
                    .Where(p => p.Id == l.LoanProductId)
                    .Select(p => p.Name)
                    .FirstOrDefault() ?? "Desconocido",
                l.PrincipalAmount,
                l.InterestRate,
                l.InterestType,
                l.Frequency,
                l.TermDays,
                l.NumberOfPayments,
                l.DisbursementDate,
                l.FirstPaymentDate,
                l.Commission,
                l.TotalInterest,
                l.TotalAmount,
                l.PaidAmount,
                l.BalanceAmount,
                l.Status,
                l.ApprovedDate,
                l.DisbursedDate,
                l.ClosedDate,
                l.Notes,
                l.CreatedAt,
                l.UpdatedAt,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (loan is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new LoanDetails(
            loan.Id,
            loan.LoanNumber,
            loan.CustomerId,
            loan.CustomerName,
            loan.CustomerPhotoUrl,
            loan.CustomerPhone,
            loan.LoanProductId,
            loan.ProductName,
            loan.PrincipalAmount.Amount,
            loan.InterestRate,
            loan.InterestType.ToString(),
            loan.Frequency.ToString(),
            loan.TermDays,
            loan.NumberOfPayments,
            loan.DisbursementDate,
            loan.FirstPaymentDate,
            loan.Commission?.Amount,
            loan.TotalInterest.Amount,
            loan.TotalAmount.Amount,
            loan.PaidAmount.Amount,
            loan.BalanceAmount.Amount,
            loan.PrincipalAmount.Currency.Code,
            loan.Status.ToString(),
            loan.ApprovedDate,
            loan.DisbursedDate,
            loan.ClosedDate,
            loan.Notes,
            loan.CreatedAt,
            loan.UpdatedAt));
    }

    private static async Task<IResult> DeleteLoanAsync(
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
        var loan = await dbContext.Loans
            .Include(l => l.Schedule)
            .Include(l => l.Payments)
            .SingleOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId, cancellationToken);

        if (loan is null)
        {
            return TypedResults.NotFound();
        }

        if (loan.Status is not (LoanStatus.Pending or LoanStatus.Approved or LoanStatus.Rejected or LoanStatus.Cancelled))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Préstamo con movimientos financieros",
                detail: "Solo se pueden eliminar préstamos pendientes, aprobados sin desembolso, rechazados o cancelados. Los préstamos desembolsados conservan historial financiero.");
        }

        if (loan.Payments.Count > 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Préstamo con pagos registrados",
                detail: "No se puede eliminar un préstamo que tiene pagos registrados.");
        }

        dbContext.Loans.Remove(loan);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                tenantId,
                currentUser.UserId,
                "loan.deleted",
                "Loan",
                id.ToString()),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreateLoanAsync(
        CreateLoanRequest request,
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

        var product = await dbContext.LoanProducts
            .SingleOrDefaultAsync(p => p.Id == request.LoanProductId && p.TenantId == currentUser.TenantId.Value, cancellationToken);

        if (product is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Producto no encontrado",
                detail: "El producto de préstamo especificado no existe.");
        }

        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(c => c.Id == request.CustomerId && c.TenantId == currentUser.TenantId.Value, cancellationToken);

        if (customer is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Cliente no encontrado",
                detail: "El cliente especificado no existe.");
        }

        // Validar tasa de interés personalizada si se proporciona
        decimal interestRate = product.InterestRate;
        if (request.CustomInterestRate.HasValue)
        {
            if (request.CustomInterestRate.Value < 0 || request.CustomInterestRate.Value > 100)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Tasa inválida",
                    detail: "La tasa de interés debe estar entre 0 y 100.");
            }
            interestRate = request.CustomInterestRate.Value;
        }

        Loan loan;
        try
        {
            var loanNumber = await GenerateLoanNumberAsync(dbContext, currentUser.TenantId.Value, cancellationToken);
            var principalAmount = Money.Of(request.PrincipalAmount, product.MinimumAmount.Currency);
            Money? commission = request.CommissionAmount.HasValue
                ? Money.Of(request.CommissionAmount.Value, product.MinimumAmount.Currency)
                : null;

            loan = Loan.Create(
                currentUser.TenantId.Value,
                request.CustomerId,
                request.LoanProductId,
                loanNumber,
                principalAmount,
                interestRate,
                product.InterestType,
                product.Frequency,
                request.TermDays,
                request.NumberOfPayments,
                request.DisbursementDate,
                request.FirstPaymentDate,
                commission,
                clock.UtcNow);

            loan.GenerateSchedule(clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Datos inválidos",
                detail: exception.Message);
        }

        // Agregar el préstamo al contexto después de generar el schedule
        dbContext.Loans.Add(loan);
        
        // Guardar cambios
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "loan.created",
                "Loan",
                loan.Id.ToString()),
            cancellationToken);

        return TypedResults.Created($"/api/v1/loans/{loan.Id}", new { id = loan.Id, loanNumber = loan.LoanNumber });
    }

    private static async Task<IResult> ApproveLoanAsync(
        Guid id,
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
            .SingleOrDefaultAsync(l => l.Id == id && l.TenantId == currentUser.TenantId.Value, cancellationToken);

        if (loan is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            loan.Approve(currentUser.UserId.Value, clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "No se puede aprobar",
                detail: exception.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "loan.approved",
                "Loan",
                loan.Id.ToString()),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> RejectLoanAsync(
        Guid id,
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

        var loan = await dbContext.Loans
            .SingleOrDefaultAsync(l => l.Id == id && l.TenantId == currentUser.TenantId.Value, cancellationToken);

        if (loan is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            loan.Reject(clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "No se puede rechazar",
                detail: exception.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "loan.rejected",
                "Loan",
                loan.Id.ToString()),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> DisburseLoanAsync(
        Guid id,
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
            .SingleOrDefaultAsync(l => l.Id == id && l.TenantId == currentUser.TenantId.Value, cancellationToken);

        if (loan is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            loan.Disburse(currentUser.UserId.Value, clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "No se puede desembolsar",
                detail: exception.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "loan.disbursed",
                "Loan",
                loan.Id.ToString()),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> RegisterPaymentAsync(
        Guid id,
        RegisterPaymentRequest request,
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
            .Include(l => l.Schedule)
            .Include(l => l.Payments)
            .SingleOrDefaultAsync(l => l.Id == id && l.TenantId == currentUser.TenantId.Value, cancellationToken);

        if (loan is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            var amount = Money.Of(request.Amount, loan.PrincipalAmount.Currency);
            var payment = loan.RegisterPayment(
                request.ReceiptNumber,
                amount,
                request.PaymentDate,
                currentUser.UserId.Value,
                clock.UtcNow);

            dbContext.Entry(payment).State = EntityState.Added;
            dbContext.Entry(payment).Reference(p => p.Amount).TargetEntry!.State = EntityState.Added;
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "No se puede registrar el pago",
                detail: exception.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "loan.payment.registered",
                "Loan",
                loan.Id.ToString()),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetLoanScheduleAsync(
        Guid id,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var tenantId = currentUser.TenantId.Value;
        var loanExists = await dbContext.Loans
            .AsNoTracking()
            .AnyAsync(l => l.Id == id && l.TenantId == tenantId, cancellationToken);

        if (!loanExists)
        {
            return TypedResults.NotFound();
        }

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var schedule = await dbContext.Loans
            .AsNoTracking()
            .Where(l => l.Id == id && l.TenantId == tenantId)
            .SelectMany(l => l.Schedule)
            .OrderBy(item => item.PaymentNumber)
            .Select(item => new
            {
                item.Id,
                item.PaymentNumber,
                item.DueDate,
                item.DueAmount,
                item.PaidAmount,
                item.LateFee,
                item.PaidDate,
                item.IsCancelled,
                item.CancelledDate,
                item.PrincipalAmount,
                item.InterestAmount,
                item.RemainingBalance,
            })
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(schedule
            .Select(item => new PaymentScheduleItemSummary(
                item.Id,
                item.PaymentNumber,
                item.DueDate,
                item.DueAmount.Amount,
                item.PaidAmount.Amount,
                item.LateFee.Amount,
                item.PaidDate,
                item.PaidAmount.Amount >= item.DueAmount.Amount,
                !item.IsCancelled && item.PaidAmount.Amount < item.DueAmount.Amount && item.DueDate < today,
                item.IsCancelled,
                item.CancelledDate,
                item.PrincipalAmount?.Amount,
                item.InterestAmount?.Amount,
                item.RemainingBalance?.Amount))
            .ToArray());
    }

private static async Task<IResult> DownloadLoanSchedulePdfAsync(
    Guid id,
    ICurrentUser currentUser,
    LendingDbContext dbContext,
    IPaymentSchedulePdfGenerator pdfGenerator,
    IClock clock,
    CancellationToken cancellationToken)
{
    if (!currentUser.TenantId.HasValue)
    {
        return TenantRequired();
    }

    var tenantId = currentUser.TenantId.Value;

    var loanData = await dbContext.Loans
        .AsNoTracking()
        .Where(l => l.Id == id && l.TenantId == tenantId)
        .Select(l => new
        {
            l.LoanNumber,

            CustomerName = dbContext.Customers
                .Where(c =>
                    c.Id == l.CustomerId &&
                    c.TenantId == tenantId)
                .Select(c => c.FullName)
                .FirstOrDefault() ?? "Cliente desconocido",

            CustomerDocument = dbContext.Customers
                .Where(c =>
                    c.Id == l.CustomerId &&
                    c.TenantId == tenantId)
                .Select(c => c.DocumentType + " " + c.DocumentNumber)
                .FirstOrDefault() ?? "No disponible",

            CustomerPhone = dbContext.Customers
                .Where(c =>
                    c.Id == l.CustomerId &&
                    c.TenantId == tenantId)
                .Select(c => c.Phone)
                .FirstOrDefault(),

            ProductName = dbContext.LoanProducts
                .Where(p => p.Id == l.LoanProductId)
                .Select(p => p.Name)
                .FirstOrDefault() ?? "Desconocido",

            l.PrincipalAmount,
            l.InterestRate,
            l.InterestType,
            l.Frequency,
            l.TermDays,
            l.NumberOfPayments,
            l.DisbursementDate,
            l.FirstPaymentDate,
            l.Commission,
            l.TotalInterest,
            l.TotalAmount,
            l.PaidAmount,
            l.BalanceAmount,
            l.Status,

            TenantName = dbContext.Tenants
                .Where(t => t.Id == tenantId)
                .Select(t => t.Name)
                .FirstOrDefault() ?? "Organización",

            TenantLogoUrl = dbContext.Tenants
                .Where(t => t.Id == tenantId)
                .Select(t => t.LogoUrl)
                .FirstOrDefault(),

            Schedule = l.Schedule
                .OrderBy(s => s.PaymentNumber)
                .Select(s => new
                {
                    s.PaymentNumber,
                    s.DueDate,
                    s.DueAmount,
                    s.PaidAmount,
                    s.PrincipalAmount,
                    s.InterestAmount,
                    s.RemainingBalance,
                    s.IsCancelled
                })
                .ToList()
        })
        .FirstOrDefaultAsync(cancellationToken);

    if (loanData is null)
    {
        return TypedResults.NotFound();
    }

    var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
    var scheduleItems = loanData.Schedule
        .Select(item =>
        {
            var dueAmount = item.DueAmount.Amount;
            var paidAmount = item.PaidAmount.Amount;
            var isPaid = paidAmount >= dueAmount;

            return new PaymentScheduleItemData(
                item.PaymentNumber,
                item.DueDate,
                item.PrincipalAmount?.Amount,
                item.InterestAmount?.Amount,
                dueAmount,
                paidAmount,
                Math.Max(0m, dueAmount - paidAmount),
                item.RemainingBalance?.Amount,
                isPaid,
                !item.IsCancelled && !isPaid && item.DueDate < today,
                item.IsCancelled);
        })
        .ToList();

    var data = new PaymentScheduleData(
        loanData.TenantName,
        loanData.TenantLogoUrl,
        loanData.LoanNumber,
        loanData.CustomerName,
        loanData.CustomerDocument,
        loanData.CustomerPhone,
        loanData.ProductName,
        loanData.PrincipalAmount.Amount,
        loanData.InterestRate,
        loanData.InterestType.ToString(),
        loanData.Frequency.ToString(),
        loanData.TermDays,
        loanData.NumberOfPayments,
        loanData.DisbursementDate,
        loanData.FirstPaymentDate,
        loanData.Commission?.Amount,
        loanData.TotalInterest.Amount,
        loanData.TotalAmount.Amount,
        loanData.PaidAmount.Amount,
        loanData.BalanceAmount.Amount,
        loanData.PrincipalAmount.Currency.Code,
        loanData.Status.ToString(),
        scheduleItems);

    var pdfBytes = await pdfGenerator.GeneratePdfAsync(
        data,
        cancellationToken);

    return Results.File(
        pdfBytes,
        "application/pdf",
        $"Cronograma_{loanData.LoanNumber}_{clock.UtcNow:yyyyMMdd}.pdf");
}

    private static async Task<IResult> CalculateSettlementAsync(
        Guid id,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IClock clock,
        DateOnly? settlementDate,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var tenantId = currentUser.TenantId.Value;
        var loan = await dbContext.Loans
            .Include(l => l.Schedule)
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId, cancellationToken);

        if (loan == null)
        {
            return TypedResults.NotFound();
        }

        if (loan.Status != LoanStatus.Active)
        {
            return TypedResults.BadRequest("Solo se pueden liquidar préstamos activos.");
        }

        var calculationDate = settlementDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var outstandingPrincipal = loan.CalculateOutstandingPrincipal();
        var accruedInterest = loan.CalculateAccruedInterest(calculationDate);
        var totalSettlement = loan.CalculateEarlySettlementAmount(calculationDate);

        return Results.Ok(new
        {
            LoanId = loan.Id,
            LoanNumber = loan.LoanNumber,
            CalculationDate = calculationDate,
            OutstandingPrincipal = outstandingPrincipal.Amount,
            AccruedInterest = accruedInterest.Amount,
            TotalSettlementAmount = totalSettlement.Amount,
            Currency = loan.PrincipalAmount.Currency.Code,
            Savings = loan.TotalInterest.Amount - accruedInterest.Amount
        });
    }

    private static async Task<IResult> SettleLoanAsync(
        Guid id,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IClock clock,
        SettleLoanRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var tenantId = currentUser.TenantId.Value;
        var loan = await dbContext.Loans
            .Include(l => l.Schedule)
            .Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId, cancellationToken);

        if (loan == null)
        {
            return TypedResults.NotFound();
        }

        var settlementAmount = Money.Of(request.Amount, loan.PrincipalAmount.Currency);
        var outstandingPrincipal = loan.CalculateOutstandingPrincipal();
        var accruedInterest = loan.CalculateAccruedInterest(request.SettlementDate);
        var totalSettlement = Money.Of(outstandingPrincipal.Amount + accruedInterest.Amount, loan.PrincipalAmount.Currency);

        // Crear registro de liquidación
        var earlySettlement = EarlySettlement.Create(
            tenantId,
            loan.Id,
            outstandingPrincipal,
            accruedInterest,
            totalSettlement,
            settlementAmount,
            request.SettlementDate,
            request.Notes,
            clock.UtcNow);

        dbContext.EarlySettlements.Add(earlySettlement);

        // Liquidar el préstamo
        loan.SettleEarly(settlementAmount, request.SettlementDate, currentUser.UserId.GetValueOrDefault(), clock.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            loan.Id,
            loan.LoanNumber,
            loan.Status,
            SettlementDate = request.SettlementDate,
            AmountPaid = settlementAmount.Amount,
            OutstandingPrincipal = outstandingPrincipal.Amount,
            AccruedInterest = accruedInterest.Amount
        });
    }

    private static async Task<IResult> SimulateRefinancingAsync(
        Guid id,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IClock clock,
        SimulateRefinancingRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var tenantId = currentUser.TenantId.Value;
        var loan = await dbContext.Loans
            .Include(l => l.Schedule)
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId, cancellationToken);

        if (loan == null)
        {
            return TypedResults.NotFound();
        }

        if (loan.Status != LoanStatus.Active)
        {
            return TypedResults.BadRequest("Solo se pueden refinanciar préstamos activos.");
        }

        var refinancingDate = request.RefinancingDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var outstandingPrincipal = loan.CalculateOutstandingPrincipal();
        var accruedInterest = loan.CalculateAccruedInterest(refinancingDate);
        var totalDebt = Money.Of(outstandingPrincipal.Amount + accruedInterest.Amount, loan.PrincipalAmount.Currency);

        var additionalAmount = request.AdditionalAmount ?? 0m;
        var newPrincipal = Money.Of(totalDebt.Amount + additionalAmount, loan.PrincipalAmount.Currency);
        var newInterestRate = request.NewInterestRate ?? loan.InterestRate;

        // Calcular nuevo cronograma basado en los parámetros
        var newNumberOfPayments = request.NewTermMonths ?? loan.NumberOfPayments;
        var newTotalInterest = newPrincipal.Amount * (newInterestRate / 100);
        var newTotalAmount = newPrincipal.Amount + newTotalInterest;
        var newPaymentAmount = newTotalAmount / newNumberOfPayments;

        return Results.Ok(new
        {
            OriginalLoanId = loan.Id,
            OriginalLoanNumber = loan.LoanNumber,
            RefinancingDate = refinancingDate,
            OutstandingPrincipal = outstandingPrincipal.Amount,
            AccruedInterest = accruedInterest.Amount,
            TotalDebt = totalDebt.Amount,
            AdditionalAmount = additionalAmount,
            NewPrincipal = newPrincipal.Amount,
            NewInterestRate = newInterestRate,
            NewTermMonths = newNumberOfPayments,
            NewPaymentAmount = newPaymentAmount,
            NewTotalAmount = newTotalAmount,
            Currency = loan.PrincipalAmount.Currency.Code
        });
    }

    private static async Task<IResult> RefinanceLoanAsync(
        Guid id,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        IClock clock,
        RefinanceLoanRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var tenantId = currentUser.TenantId.Value;
        var originalLoan = await dbContext.Loans
            .Include(l => l.Schedule)
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId, cancellationToken);

        if (originalLoan == null)
        {
            return TypedResults.NotFound();
        }

        if (originalLoan.Status != LoanStatus.Active)
        {
            return TypedResults.BadRequest("Solo se pueden refinanciar préstamos activos.");
        }

        // Calcular deuda total
        var outstandingPrincipal = originalLoan.CalculateOutstandingPrincipal();
        var accruedInterest = originalLoan.CalculateAccruedInterest(request.RefinancingDate);
        var totalDebt = Money.Of(outstandingPrincipal.Amount + accruedInterest.Amount, originalLoan.PrincipalAmount.Currency);

        var additionalAmount = request.AdditionalAmount ?? 0m;
        var newPrincipal = Money.Of(totalDebt.Amount + additionalAmount, originalLoan.PrincipalAmount.Currency);
        
        // Obtener el producto de préstamo
        var loanProduct = await dbContext.LoanProducts
            .FirstOrDefaultAsync(p => p.Id == request.NewLoanProductId && p.TenantId == tenantId, cancellationToken);

        if (loanProduct == null)
        {
            return TypedResults.NotFound();
        }

        // Generar número de préstamo
        var loanNumber = await GenerateLoanNumberAsync(dbContext, tenantId, cancellationToken);

        // Crear nuevo préstamo
        var newLoan = Loan.Create(
            tenantId,
            originalLoan.CustomerId,
            loanProduct.Id,
            loanNumber,
            newPrincipal,
            request.NewInterestRate,
            loanProduct.InterestType,
            loanProduct.Frequency,
            request.NewTermDays,
            request.NewNumberOfPayments,
            request.RefinancingDate,
            request.FirstPaymentDate,
            null,
            clock.UtcNow);

        // Vincular como refinanciamiento
        newLoan.SetAsRefinancingOf(originalLoan.Id, clock.UtcNow);

        // Generar cronograma del nuevo préstamo
        newLoan.GenerateSchedule(clock.UtcNow);

        // Aprobar y desembolsar automáticamente el nuevo préstamo
        newLoan.Approve(currentUser.UserId.GetValueOrDefault(), clock.UtcNow);
        newLoan.Disburse(currentUser.UserId.GetValueOrDefault(), clock.UtcNow);

        dbContext.Loans.Add(newLoan);

        // Marcar el préstamo original como refinanciado
        originalLoan.MarkAsRefinanced(request.RefinancingDate, clock.UtcNow);

        // Crear registro de refinanciamiento
        var refinancing = LoanRefinancing.Create(
            tenantId,
            originalLoan.Id,
            newLoan.Id,
            outstandingPrincipal,
            accruedInterest,
            totalDebt,
            newPrincipal,
            request.NewInterestRate,
            request.RefinancingDate,
            request.Notes,
            clock.UtcNow);

        dbContext.LoanRefinancings.Add(refinancing);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/loans/{newLoan.Id}", new
        {
            OriginalLoanId = originalLoan.Id,
            OriginalLoanNumber = originalLoan.LoanNumber,
            NewLoanId = newLoan.Id,
            NewLoanNumber = newLoan.LoanNumber,
            RefinancingDate = request.RefinancingDate,
            OutstandingPrincipal = outstandingPrincipal.Amount,
            AccruedInterest = accruedInterest.Amount,
            TotalDebt = totalDebt.Amount,
            NewPrincipal = newPrincipal.Amount,
            NewInterestRate = request.NewInterestRate
        });
    }

    private static async Task<string> GenerateLoanNumberAsync(
        LendingDbContext dbContext,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"L{year}";
        
        var lastNumber = await dbContext.Loans
            .Where(l => l.TenantId == tenantId && l.LoanNumber.StartsWith(prefix))
            .OrderByDescending(l => l.LoanNumber)
            .Select(l => l.LoanNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int sequence = 1;
        if (lastNumber is not null)
        {
            var parts = lastNumber.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var lastSeq))
            {
                sequence = lastSeq + 1;
            }
        }

        return $"{prefix}-{sequence:D6}";
    }

    private static ProblemHttpResult TenantRequired() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Organización requerida",
            detail: "Selecciona una organización antes de continuar.");

    public sealed record LoanSummary(
        Guid Id,
        string LoanNumber,
        Guid CustomerId,
        string CustomerName,
        string? CustomerPhotoUrl,
        decimal PrincipalAmount,
        decimal TotalAmount,
        decimal BalanceAmount,
        string Currency,
        decimal InterestRate,
        string Status,
        DateOnly DisbursementDate,
        DateTimeOffset CreatedAt);

    public sealed record LoanDetails(
        Guid Id,
        string LoanNumber,
        Guid CustomerId,
        string CustomerName,
        string? CustomerPhotoUrl,
        string? CustomerPhone,
        Guid LoanProductId,
        string ProductName,
        decimal PrincipalAmount,
        decimal InterestRate,
        string InterestType,
        string Frequency,
        int TermDays,
        int NumberOfPayments,
        DateOnly DisbursementDate,
        DateOnly FirstPaymentDate,
        decimal? Commission,
        decimal TotalInterest,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal BalanceAmount,
        string Currency,
        string Status,
        DateOnly? ApprovedDate,
        DateOnly? DisbursedDate,
        DateOnly? ClosedDate,
        string? Notes,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed record CreateLoanRequest(
        Guid CustomerId,
        Guid LoanProductId,
        decimal PrincipalAmount,
        int TermDays,
        int NumberOfPayments,
        DateOnly DisbursementDate,
        DateOnly FirstPaymentDate,
        decimal? CommissionAmount,
        decimal? CustomInterestRate);

    public sealed record RegisterPaymentRequest(
        string ReceiptNumber,
        decimal Amount,
        DateOnly PaymentDate);

    public sealed record PaymentScheduleItemSummary(
        Guid Id,
        int PaymentNumber,
        DateOnly DueDate,
        decimal DueAmount,
        decimal PaidAmount,
        decimal LateFee,
        DateOnly? PaidDate,
        bool IsPaid,
        bool IsOverdue,
        bool IsCancelled,
        DateOnly? CancelledDate,
        decimal? PrincipalAmount,
        decimal? InterestAmount,
        decimal? RemainingBalance);

    public sealed record SettleLoanRequest(
        decimal Amount,
        DateOnly SettlementDate,
        string? Notes);

    public sealed record SimulateRefinancingRequest(
        DateOnly? RefinancingDate,
        decimal? AdditionalAmount,
        decimal? NewInterestRate,
        int? NewTermMonths);

    public sealed record RefinanceLoanRequest(
        Guid NewLoanProductId,
        decimal NewInterestRate,
        int NewTermDays,
        int NewNumberOfPayments,
        DateOnly RefinancingDate,
        DateOnly FirstPaymentDate,
        decimal? AdditionalAmount,
        string? Notes);
}
