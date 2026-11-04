using Lending.Domain.Common;
using Lending.Domain.ValueObjects;

namespace Lending.Domain.Cash;

/// <summary>
/// Transacción de caja (ingreso o egreso)
/// </summary>
public sealed class CashTransaction : Entity
{
    private CashTransaction()
    {
    }

    internal CashTransaction(
        Guid sessionId,
        CashTransactionType type,
        CashFlowDirection direction,
        Money amount,
        string? description,
        Guid? loanId,
        DateTimeOffset registeredAt)
    {
        SessionId = sessionId;
        Type = type;
        Direction = direction;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Description = description;
        LoanId = loanId;
        RegisteredAt = registeredAt;
    }

    public Guid SessionId { get; private set; }

    public CashTransactionType Type { get; private set; }

    public CashFlowDirection Direction { get; private set; }

    public Money Amount { get; private set; } = null!;

    public string? Description { get; private set; }

    public Guid? LoanId { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }
}
