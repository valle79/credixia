using Lending.Domain.Common;
using Lending.Domain.ValueObjects;

namespace Lending.Domain.Cash;

/// <summary>
/// Sesión de caja diaria para un cajero
/// </summary>
public sealed class CashSession : Entity
{
    private readonly List<CashTransaction> _transactions = [];

    private CashSession()
    {
    }

    private CashSession(
        Guid tenantId,
        Guid userId,
        Money openingBalance,
        DateOnly sessionDate,
        DateTimeOffset openedAt)
    {
        TenantId = tenantId;
        UserId = userId;
        OpeningBalance = openingBalance ?? throw new ArgumentNullException(nameof(openingBalance));
        SessionDate = sessionDate;
        OpenedAt = openedAt;
        CurrentBalance = openingBalance;
        Status = CashSessionStatus.Open;
    }

    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public DateOnly SessionDate { get; private set; }

    public Money OpeningBalance { get; private set; } = null!;

    public Money CurrentBalance { get; private set; } = null!;

    public Money? ClosingBalance { get; private set; }

    public Money? PhysicalCount { get; private set; }

    public Money? Difference { get; private set; }

    public CashSessionStatus Status { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyCollection<CashTransaction> Transactions => _transactions;

    public static CashSession Open(
        Guid tenantId,
        Guid userId,
        Money openingBalance,
        DateOnly sessionDate,
        DateTimeOffset now)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        EnsureTimestamp(now);
        return new CashSession(tenantId, userId, openingBalance, sessionDate, now);
    }

    public void RegisterInflow(Money amount, CashTransactionType type, string? description, Guid? loanId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(amount);
        EnsureTimestamp(now);
        EnsureOpen();

        if (amount.Amount <= 0)
        {
            throw new DomainException("cash.inflow.amount.invalid", "El monto de ingreso debe ser mayor a cero.");
        }

        var transaction = new CashTransaction(
            Id,
            type,
            CashFlowDirection.Inflow,
            amount,
            description,
            loanId,
            now);

        _transactions.Add(transaction);
        CurrentBalance = CurrentBalance.Add(amount);
    }

    public void RegisterOutflow(Money amount, CashTransactionType type, string? description, Guid? loanId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(amount);
        EnsureTimestamp(now);
        EnsureOpen();

        if (amount.Amount <= 0)
        {
            throw new DomainException("cash.outflow.amount.invalid", "El monto de salida debe ser mayor a cero.");
        }

        if (CurrentBalance.Amount < amount.Amount)
        {
            throw new DomainException("cash.outflow.insufficient_balance", "Saldo insuficiente en caja.");
        }

        var transaction = new CashTransaction(
            Id,
            type,
            CashFlowDirection.Outflow,
            amount,
            description,
            loanId,
            now);

        _transactions.Add(transaction);
        CurrentBalance = CurrentBalance.Subtract(amount);
    }

    public void Close(Money physicalCount, string? notes, DateTimeOffset now)
    {
        EnsureTimestamp(now);
        EnsureOpen();

        PhysicalCount = physicalCount ?? throw new ArgumentNullException(nameof(physicalCount));
        ClosingBalance = CurrentBalance;
        Difference = PhysicalCount.Subtract(ClosingBalance);
        Status = CashSessionStatus.Closed;
        ClosedAt = now;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    private void EnsureOpen()
    {
        if (Status != CashSessionStatus.Open)
        {
            throw new DomainException("cash.session.not_open", "La sesión de caja no está abierta.");
        }
    }

    private static void EnsureTimestamp(DateTimeOffset now)
    {
        if (now == default)
        {
            throw new ArgumentException("A valid timestamp is required.", nameof(now));
        }
    }
}
