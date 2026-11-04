using Lending.Domain.Common;

namespace Lending.Domain.Collections;

/// <summary>
/// Acción de cobranza registrada
/// </summary>
public sealed class CollectionAction : Entity
{
    private CollectionAction()
    {
    }

    internal CollectionAction(
        Guid loanId,
        Guid collectorUserId,
        CollectionActionType type,
        string? notes,
        DateTimeOffset registeredAt)
    {
        LoanId = loanId;
        CollectorUserId = collectorUserId;
        Type = type;
        Notes = notes;
        RegisteredAt = registeredAt;
    }

    public Guid LoanId { get; private set; }

    public Guid CollectorUserId { get; private set; }

    public CollectionActionType Type { get; private set; }

    public string? Notes { get; private set; }

    public DateOnly? PromiseDate { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public static CollectionAction Create(
        Guid loanId,
        Guid collectorUserId,
        CollectionActionType type,
        string? notes,
        DateOnly? promiseDate,
        DateTimeOffset now)
    {
        if (loanId == Guid.Empty)
        {
            throw new ArgumentException("Loan identifier cannot be empty.", nameof(loanId));
        }

        if (collectorUserId == Guid.Empty)
        {
            throw new ArgumentException("Collector identifier cannot be empty.", nameof(collectorUserId));
        }

        if (now == default)
        {
            throw new ArgumentException("A valid timestamp is required.", nameof(now));
        }

        return new CollectionAction(loanId, collectorUserId, type, notes, now)
        {
            PromiseDate = promiseDate,
        };
    }

    internal void SetPromiseDate(DateOnly promiseDate)
    {
        PromiseDate = promiseDate;
    }
}
