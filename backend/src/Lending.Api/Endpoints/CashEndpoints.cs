using Lending.Api.Authentication;
using Lending.Api.Authorization;
using Lending.Application.Abstractions;
using Lending.Application.Common;
using Lending.Domain.Cash;
using Lending.Domain.Common;
using Lending.Domain.Identity;
using Lending.Domain.ValueObjects;
using Lending.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lending.Api.Endpoints;

public static class CashEndpoints
{
    public static IEndpointRouteBuilder MapCashEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/cash")
            .WithTags("Cash")
            .RequireAuthorization();

        group.MapGet("/", ListCashSessionsAsync)
            .WithName("ListCashSessions")
            .RequirePermission(Permissions.CashManage);

        group.MapGet("/current", GetCurrentSessionAsync)
            .WithName("GetCurrentCashSession")
            .RequirePermission(Permissions.CashManage);

        group.MapGet("/{id:guid}", GetCashSessionAsync)
            .WithName("GetCashSession")
            .RequirePermission(Permissions.CashManage);

        group.MapPost("/open", OpenCashSessionAsync)
            .WithName("OpenCashSession")
            .RequirePermission(Permissions.CashManage);

        group.MapPost("/{id:guid}/close", CloseCashSessionAsync)
            .WithName("CloseCashSession")
            .RequirePermission(Permissions.CashManage);

        group.MapGet("/{id:guid}/transactions", ListCashTransactionsAsync)
            .WithName("ListCashTransactions")
            .RequirePermission(Permissions.CashManage);

        return endpoints;
    }

    private static async Task<IResult> ListCashSessionsAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        string? status,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var query = dbContext.CashSessions
            .AsNoTracking()
            .Where(cs => cs.TenantId == currentUser.TenantId.Value);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<CashSessionStatus>(status, true, out var statusEnum))
        {
            query = query.Where(cs => cs.Status == statusEnum);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize, 20, 100);

        var sessions = await query
            .OrderByDescending(cs => cs.OpenedAt)
            .ThenByDescending(cs => cs.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(cs => new CashSessionRow(
                cs.Id,
                cs.UserId,
                cs.SessionDate,
                cs.OpenedAt,
                cs.ClosedAt,
                cs.OpeningBalance,
                cs.CurrentBalance,
                cs.ClosingBalance,
                cs.PhysicalCount,
                cs.Difference,
                cs.Transactions
                    .Where(t => t.Direction == CashFlowDirection.Inflow)
                    .Sum(t => (decimal?)t.Amount.Amount) ?? 0m,
                cs.Transactions
                    .Where(t => t.Direction == CashFlowDirection.Outflow)
                    .Sum(t => (decimal?)t.Amount.Amount) ?? 0m,
                cs.Status,
                cs.Notes))
            .ToListAsync(cancellationToken);

        var users = await GetUserNamesAsync(
            dbContext,
            sessions.Select(cs => cs.UserId).Distinct().ToArray(),
            cancellationToken);

        return TypedResults.Ok(new PagedResult<CashSessionDto>(
            sessions.Select(cs => ToDto(cs, users)).ToArray(),
            page.PageNumber,
            page.PageSize,
            totalCount));
    }

    private static async Task<IResult> GetCurrentSessionAsync(
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var query = dbContext.CashSessions
            .AsNoTracking()
            .Where(cs => cs.TenantId == currentUser.TenantId.Value &&
                cs.Status == CashSessionStatus.Open);

        if (currentUser.UserId.HasValue)
        {
            query = query.Where(cs => cs.UserId == currentUser.UserId.Value);
        }

        var session = await query
            .OrderByDescending(cs => cs.OpenedAt)
            .Select(cs => new CashSessionRow(
                cs.Id,
                cs.UserId,
                cs.SessionDate,
                cs.OpenedAt,
                cs.ClosedAt,
                cs.OpeningBalance,
                cs.CurrentBalance,
                cs.ClosingBalance,
                cs.PhysicalCount,
                cs.Difference,
                cs.Transactions
                    .Where(t => t.Direction == CashFlowDirection.Inflow)
                    .Sum(t => (decimal?)t.Amount.Amount) ?? 0m,
                cs.Transactions
                    .Where(t => t.Direction == CashFlowDirection.Outflow)
                    .Sum(t => (decimal?)t.Amount.Amount) ?? 0m,
                cs.Status,
                cs.Notes))
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return TypedResults.Ok<CashSessionDto?>(null);
        }

        var users = await GetUserNamesAsync(dbContext, [session.UserId], cancellationToken);
        return TypedResults.Ok<CashSessionDto?>(ToDto(session, users));
    }

    private static async Task<IResult> GetCashSessionAsync(
        Guid id,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var session = await dbContext.CashSessions
            .AsNoTracking()
            .Where(cs => cs.Id == id && cs.TenantId == currentUser.TenantId.Value)
            .Select(cs => new CashSessionRow(
                cs.Id,
                cs.UserId,
                cs.SessionDate,
                cs.OpenedAt,
                cs.ClosedAt,
                cs.OpeningBalance,
                cs.CurrentBalance,
                cs.ClosingBalance,
                cs.PhysicalCount,
                cs.Difference,
                cs.Transactions
                    .Where(t => t.Direction == CashFlowDirection.Inflow)
                    .Sum(t => (decimal?)t.Amount.Amount) ?? 0m,
                cs.Transactions
                    .Where(t => t.Direction == CashFlowDirection.Outflow)
                    .Sum(t => (decimal?)t.Amount.Amount) ?? 0m,
                cs.Status,
                cs.Notes))
            .SingleOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return TypedResults.NotFound();
        }

        var users = await GetUserNamesAsync(dbContext, [session.UserId], cancellationToken);
        return TypedResults.Ok(ToDto(session, users));
    }

    private static async Task<IResult> OpenCashSessionAsync(
        OpenCashSessionRequest request,
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

        var alreadyOpen = await dbContext.CashSessions
            .AnyAsync(
                cs => cs.TenantId == currentUser.TenantId.Value &&
                    cs.UserId == currentUser.UserId.Value &&
                    cs.Status == CashSessionStatus.Open,
                cancellationToken);

        if (alreadyOpen)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Caja ya abierta",
                detail: "Ya tienes una sesión de caja abierta.");
        }

        if (request.OpeningBalance < 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Datos inválidos",
                detail: "El saldo inicial no puede ser negativo.");
        }

        CashSession session;
        try
        {
            session = CashSession.Open(
                currentUser.TenantId.Value,
                currentUser.UserId.Value,
                Money.Of(request.OpeningBalance, Currency.Pen),
                DateOnly.FromDateTime(clock.UtcNow.UtcDateTime),
                clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Datos inválidos",
                detail: exception.Message);
        }

        dbContext.CashSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "cash.session.opened",
                "CashSession",
                session.Id.ToString()),
            cancellationToken);

        var users = await GetUserNamesAsync(dbContext, [session.UserId], cancellationToken);
        return TypedResults.Created($"/api/v1/cash/{session.Id}", ToDto(session, users));
    }

    private static async Task<IResult> CloseCashSessionAsync(
        Guid id,
        CloseCashSessionRequest request,
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

        var session = await dbContext.CashSessions
            .SingleOrDefaultAsync(
                cs => cs.Id == id && cs.TenantId == currentUser.TenantId.Value,
                cancellationToken);

        if (session is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            session.Close(
                Money.Of(request.ClosingBalance, session.CurrentBalance.Currency),
                request.Notes,
                clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "No se puede cerrar la caja",
                detail: exception.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.RecordAsync(
            new AuditEntry(
                currentUser.TenantId,
                currentUser.UserId,
                "cash.session.closed",
                "CashSession",
                session.Id.ToString()),
            cancellationToken);

        var users = await GetUserNamesAsync(dbContext, [session.UserId], cancellationToken);
        return TypedResults.Ok(ToDto(session, users));
    }

    private static async Task<IResult> ListCashTransactionsAsync(
        Guid id,
        ICurrentUser currentUser,
        LendingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TenantId.HasValue)
        {
            return TenantRequired();
        }

        var session = await dbContext.CashSessions
            .AsNoTracking()
            .Where(cs => cs.Id == id && cs.TenantId == currentUser.TenantId.Value)
            .Select(cs => new { cs.UserId })
            .SingleOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return TypedResults.NotFound();
        }

        var users = await GetUserNamesAsync(dbContext, [session.UserId], cancellationToken);
        var openedBy = users.GetValueOrDefault(session.UserId, "Desconocido");

        var transactions = await dbContext.CashSessions
            .AsNoTracking()
            .Where(cs => cs.Id == id && cs.TenantId == currentUser.TenantId.Value)
            .SelectMany(cs => cs.Transactions)
            .OrderByDescending(t => t.RegisteredAt)
            .ThenByDescending(t => t.Id)
            .Select(t => new
            {
                t.Id,
                t.SessionId,
                t.Type,
                t.Direction,
                t.Amount,
                t.LoanId,
                t.Description,
                t.RegisteredAt,
            })
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(transactions
            .Select(t => new CashTransactionDto(
                t.Id,
                t.SessionId,
                t.Type.ToString(),
                t.Direction.ToString(),
                t.Amount.Amount,
                t.Amount.Currency.Code,
                t.LoanId?.ToString(),
                t.Description,
                t.RegisteredAt,
                openedBy))
            .ToArray());
    }

    private static CashSessionDto ToDto(
        CashSession session,
        IReadOnlyDictionary<Guid, string> users)
    {
        var totalIn = session.Transactions
            .Where(t => t.Direction == CashFlowDirection.Inflow)
            .Sum(t => t.Amount.Amount);
        var totalOut = session.Transactions
            .Where(t => t.Direction == CashFlowDirection.Outflow)
            .Sum(t => t.Amount.Amount);

        return new CashSessionDto(
            session.Id,
            BuildSessionNumber(session.Id, session.SessionDate),
            session.UserId,
            users.GetValueOrDefault(session.UserId, "Desconocido"),
            session.OpenedAt,
            session.ClosedAt,
            session.OpeningBalance.Amount,
            session.OpeningBalance.Currency.Code,
            session.CurrentBalance.Amount,
            session.CurrentBalance.Amount,
            session.ClosingBalance?.Amount,
            session.PhysicalCount?.Amount,
            session.Difference?.Amount,
            totalIn,
            totalOut,
            session.Status.ToString(),
            session.Notes);
    }

    private static CashSessionDto ToDto(
        CashSessionRow session,
        IReadOnlyDictionary<Guid, string> users)
    {
        return new CashSessionDto(
            session.Id,
            BuildSessionNumber(session.Id, session.SessionDate),
            session.UserId,
            users.GetValueOrDefault(session.UserId, "Desconocido"),
            session.OpenedAt,
            session.ClosedAt,
            session.OpeningBalance.Amount,
            session.OpeningBalance.Currency.Code,
            session.CurrentBalance.Amount,
            session.CurrentBalance.Amount,
            session.ClosingBalance?.Amount,
            session.PhysicalCount?.Amount,
            session.Difference?.Amount,
            session.TotalIn,
            session.TotalOut,
            session.Status.ToString(),
            session.Notes);
    }

    private static string BuildSessionNumber(Guid id, DateOnly sessionDate) =>
        $"CS-{sessionDate:yyyyMMdd}-{id.ToString()[..8].ToUpperInvariant()}";

    private static async Task<Dictionary<Guid, string>> GetUserNamesAsync(
        LendingDbContext dbContext,
        Guid[] userIds,
        CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);
    }

    private static ProblemHttpResult TenantRequired() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Organización requerida",
            detail: "Selecciona una organización antes de continuar.");

    public sealed record OpenCashSessionRequest(decimal OpeningBalance, string? Notes);

    public sealed record CloseCashSessionRequest(decimal ClosingBalance, string? Notes);

    private sealed record CashSessionRow(
        Guid Id,
        Guid UserId,
        DateOnly SessionDate,
        DateTimeOffset OpenedAt,
        DateTimeOffset? ClosedAt,
        Money OpeningBalance,
        Money CurrentBalance,
        Money? ClosingBalance,
        Money? PhysicalCount,
        Money? Difference,
        decimal TotalIn,
        decimal TotalOut,
        CashSessionStatus Status,
        string? Notes);

    public sealed record CashSessionDto(
        Guid Id,
        string SessionNumber,
        Guid OpenedByUserId,
        string OpenedBy,
        DateTimeOffset OpenedAt,
        DateTimeOffset? ClosedAt,
        decimal OpeningBalance,
        string Currency,
        decimal CurrentBalance,
        decimal ExpectedBalance,
        decimal? ClosingBalance,
        decimal? PhysicalCount,
        decimal? Difference,
        decimal TotalIn,
        decimal TotalOut,
        string Status,
        string? Notes);

    public sealed record CashTransactionDto(
        Guid Id,
        Guid CashSessionId,
        string TransactionType,
        string Direction,
        decimal Amount,
        string Currency,
        string? Reference,
        string? Notes,
        DateTimeOffset CreatedAt,
        string CreatedBy);
}
