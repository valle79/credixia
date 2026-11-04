using Lending.Domain.Common;
using Lending.Domain.ValueObjects;

namespace Lending.Domain.Loans;

/// <summary>
/// Ítem del cronograma de pagos
/// </summary>
public sealed class PaymentScheduleItem : Entity
{
    private PaymentScheduleItem()
    {
    }

    internal PaymentScheduleItem(
        Guid loanId,
        int paymentNumber,
        DateOnly dueDate,
        Money dueAmount,
        Money paidAmount,
        Money lateFee,
        Money? principalAmount = null,
        Money? interestAmount = null,
        Money? remainingBalance = null)
    {
        LoanId = loanId;
        PaymentNumber = paymentNumber;
        DueDate = dueDate;
        DueAmount = dueAmount ?? throw new ArgumentNullException(nameof(dueAmount));
        PaidAmount = paidAmount ?? throw new ArgumentNullException(nameof(paidAmount));
        LateFee = lateFee ?? throw new ArgumentNullException(nameof(lateFee));
        PrincipalAmount = principalAmount;
        InterestAmount = interestAmount;
        RemainingBalance = remainingBalance;
    }

    public Guid LoanId { get; private set; }

    public int PaymentNumber { get; private set; }

    public DateOnly DueDate { get; private set; }

    public Money DueAmount { get; private set; } = null!;

    public Money PaidAmount { get; private set; } = null!;

    public Money LateFee { get; private set; } = null!;

    public Money? PrincipalAmount { get; private set; }

    public Money? InterestAmount { get; private set; }

    public Money? RemainingBalance { get; private set; }

    public DateOnly? PaidDate { get; private set; }

    public bool IsPaid => PaidAmount.Amount >= DueAmount.Amount;

    public bool IsOverdue(DateOnly currentDate) => !IsPaid && currentDate > DueDate;

    internal void AddPayment(Money amount, DateOnly paymentDate)
    {
        PaidAmount = PaidAmount.Add(amount);
        PaidDate ??= paymentDate;
    }

    internal void AddLateFee(Money fee)
    {
        LateFee = LateFee.Add(fee);
    }
}
