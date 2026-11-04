using Lending.Api.Authorization;
using Lending.Application.Abstractions;
using Lending.Domain.Customers;
using Lending.Domain.Identity;
using Lending.Domain.Loans;
using Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lending.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/summary", GetSummaryAsync)
            .WithName("GetDashboardSummary")
            .RequirePermission(Permissions.LoansView);

        return endpoints;
    }

    private static async Task<IResult> GetSummaryAsync(
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
        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var monthStart = new DateOnly(now.Year, now.Month, 1);

        var statusAggregates = await dbContext.Loans
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .GroupBy(l => l.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count(),
                Balance = group.Sum(l => l.BalanceAmount.Amount),
                Principal = group.Sum(l => l.PrincipalAmount.Amount),
                Interest = group.Sum(l => l.TotalInterest.Amount),
            })
            .ToListAsync(cancellationToken);

        var active = statusAggregates.FirstOrDefault(item => item.Status == LoanStatus.Active);
        var pending = statusAggregates.FirstOrDefault(item => item.Status == LoanStatus.Pending);

        var disbursedThisMonth = await dbContext.Loans
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId && l.DisbursedDate >= monthStart)
            .SumAsync(l => (decimal?)l.PrincipalAmount.Amount, cancellationToken) ?? 0m;

        var activeCustomerCount = await dbContext.Customers
            .AsNoTracking()
            .CountAsync(
                c => c.TenantId == tenantId && c.Status == CustomerStatus.Active,
                cancellationToken);

        var overdueQuery = dbContext.Loans
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId &&
                (l.Status == LoanStatus.Active || l.Status == LoanStatus.DefaultedDef) &&
                l.Schedule.Any(item =>
                    item.DueDate < today && item.PaidAmount.Amount < item.DueAmount.Amount));

        var overdueAggregates = await overdueQuery
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Balance = group.Sum(l => l.BalanceAmount.Amount),
            })
            .SingleOrDefaultAsync(cancellationToken);

        var overdueLoanCount = overdueAggregates?.Count ?? 0;
        var overdueBalance = overdueAggregates?.Balance ?? 0m;

        var pendingLoanRows = await dbContext.Loans
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId && l.Status == LoanStatus.Pending)
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Take(5)
            .Select(l => new
            {
                l.Id,
                l.LoanNumber,
                l.CustomerId,
                CustomerName = dbContext.Customers
                    .Where(c => c.Id == l.CustomerId)
                    .Select(c => c.FullName)
                    .FirstOrDefault() ?? "Desconocido",
                l.PrincipalAmount,
                l.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var pendingLoans = pendingLoanRows
            .Select(l => new PendingLoanSummary(
                l.Id,
                l.LoanNumber,
                l.CustomerId,
                l.CustomerName,
                l.PrincipalAmount.Amount,
                l.PrincipalAmount.Currency.Code,
                l.CreatedAt))
            .ToArray();

        return TypedResults.Ok(new DashboardSummaryDto(
            active?.Balance ?? 0m,
            active?.Count ?? 0,
            active?.Principal ?? 0m,
            active?.Interest ?? 0m,
            disbursedThisMonth,
            activeCustomerCount,
            pending?.Count ?? 0,
            overdueBalance,
            overdueLoanCount,
            pendingLoans));
    }

    private static ProblemHttpResult TenantRequired() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Organización requerida",
            detail: "Selecciona una organización antes de continuar.");

    public sealed record DashboardSummaryDto(
        decimal ActivePortfolio,
        int ActiveLoanCount,
        decimal ActivePrincipal,
        decimal ActiveInterest,
        decimal DisbursedThisMonth,
        int ActiveCustomerCount,
        int PendingLoanCount,
        decimal OverdueBalance,
        int OverdueLoanCount,
        IReadOnlyList<PendingLoanSummary> PendingLoans);

    public sealed record PendingLoanSummary(
        Guid Id,
        string LoanNumber,
        Guid CustomerId,
        string CustomerName,
        decimal PrincipalAmount,
        string Currency,
        DateTimeOffset CreatedAt);
}
