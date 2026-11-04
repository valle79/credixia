using Lending.Domain.Common;
using Lending.Domain.ValueObjects;

namespace Lending.Domain.Loans;

/// <summary>
/// Pago realizado a un préstamo
/// </summary>
public sealed class Payment : Entity
{
    private Payment()
    {
    }

    internal Payment(
        Guid loanId,
        string receiptNumber,
        Money amount,
        DateOnly paymentDate,
        Guid receivedByUserId,
        DateTimeOffset registeredAt)
    {
        LoanId = loanId;
        ReceiptNumber = receiptNumber;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        PaymentDate = paymentDate;
        ReceivedByUserId = receivedByUserId;
        RegisteredAt = registeredAt;
    }

    public Guid LoanId { get; private set; }

    public string ReceiptNumber { get; private set; } = string.Empty;

    public Money Amount { get; private set; } = null!;

    public DateOnly PaymentDate { get; private set; }

    public Guid ReceivedByUserId { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public bool IsReversed { get; private set; }

    public DateTimeOffset? ReversedAt { get; private set; }

    public Guid? ReversedByUserId { get; private set; }

    public string? ReversalReason { get; private set; }

    internal void Reverse(Guid reversedByUserId, string reason, DateTimeOffset now)
    {
        if (IsReversed)
        {
            throw new DomainException("payment.already_reversed", "Este pago ya fue reversado.");
        }

        IsReversed = true;
        ReversedAt = now;
        ReversedByUserId = reversedByUserId;
        ReversalReason = reason;
    }
}
