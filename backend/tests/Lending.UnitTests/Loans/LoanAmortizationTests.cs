using Lending.Domain.LoanProducts;
using Lending.Domain.Loans;
using Lending.Domain.ValueObjects;

namespace Lending.UnitTests.Loans;

public sealed class LoanAmortizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly DisbursementDate = new(2026, 1, 1);
    private static readonly DateOnly FirstPaymentDate = new(2026, 2, 1);

    [Fact]
    public void French_schedule_uses_fixed_payment_and_amortizes_principal()
    {
        var loan = CreateLoan(InterestType.French, 1200m, 12m, 12);
        loan.GenerateSchedule(Now);

        var payments = loan.Schedule.OrderBy(item => item.PaymentNumber).ToArray();

        Assert.Equal(1200m, payments.Sum(item => item.PrincipalAmount!.Amount));
        Assert.Equal(loan.TotalInterest.Amount, payments.Sum(item => item.InterestAmount!.Amount));
        Assert.Equal(106.62m, payments[0].DueAmount.Amount);
        Assert.Equal(12.00m, payments[0].InterestAmount!.Amount);
        Assert.True(payments[1].InterestAmount!.Amount < payments[0].InterestAmount!.Amount);
        Assert.Equal(0m, payments[^1].RemainingBalance!.Amount);
    }

    [Fact]
    public void Simple_schedule_keeps_flat_interest()
    {
        var loan = CreateLoan(InterestType.Simple, 1200m, 12m, 12);
        loan.GenerateSchedule(Now);

        var payments = loan.Schedule.OrderBy(item => item.PaymentNumber).ToArray();

        Assert.All(payments, item => Assert.Equal(12.00m, item.InterestAmount!.Amount));
        Assert.Equal(144.00m, loan.TotalInterest.Amount);
        Assert.Equal(1200m, payments.Sum(item => item.PrincipalAmount!.Amount));
    }

    [Fact]
    public void Outstanding_principal_discounts_partial_payments_after_interest()
    {
        var loan = CreateLoan(InterestType.French, 1200m, 12m, 12);
        loan.GenerateSchedule(Now);
        loan.Approve(Guid.NewGuid(), Now);
        loan.Disburse(Guid.NewGuid(), Now);

        loan.RegisterPayment("REC-1", Money.Of(50m, Currency.Pen), new DateOnly(2026, 1, 20), Guid.NewGuid(), Now);

        Assert.Equal(1162m, loan.CalculateOutstandingPrincipal().Amount);
    }

    [Fact]
    public void Accrued_interest_is_prorated_by_contractual_period_for_french_loans()
    {
        var loan = CreateLoan(InterestType.French, 1200m, 12m, 12);
        loan.GenerateSchedule(Now);

        var accrued = loan.CalculateAccruedInterest(new DateOnly(2026, 1, 16));

        Assert.Equal(5.81m, accrued.Amount);
    }

    [Fact]
    public void Early_settlement_cancels_pending_installments_and_clears_balance()
    {
        var loan = CreateLoan(InterestType.French, 1200m, 12m, 12);
        loan.GenerateSchedule(Now);
        loan.Approve(Guid.NewGuid(), Now);
        loan.Disburse(Guid.NewGuid(), Now);

        var settlementDate = new DateOnly(2026, 1, 16);
        var settlementAmount = loan.CalculateEarlySettlementAmount(settlementDate);
        loan.SettleEarly(settlementAmount, settlementDate, Guid.NewGuid(), Now);

        Assert.Equal(LoanStatus.SettledEarly, loan.Status);
        Assert.Equal(0m, loan.BalanceAmount.Amount);
        Assert.Equal(0m, loan.CalculateOutstandingPrincipal().Amount);
        Assert.All(loan.Schedule, item => Assert.True(item.IsCancelled));
        Assert.All(loan.Schedule, item => Assert.Equal(0m, item.PaidAmount.Amount));
    }

    private static Loan CreateLoan(InterestType interestType, decimal principal, decimal rate, int payments)
    {
        return Loan.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            $"TEST-{Guid.NewGuid():N}",
            Money.Of(principal, Currency.Pen),
            rate,
            interestType,
            PaymentFrequency.Monthly,
            365,
            payments,
            DisbursementDate,
            FirstPaymentDate,
            null,
            Now);
    }
}
