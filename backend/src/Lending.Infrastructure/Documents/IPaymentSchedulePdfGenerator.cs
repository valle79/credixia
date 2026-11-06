namespace Lending.Infrastructure.Documents;

public interface IPaymentSchedulePdfGenerator
{
    Task<byte[]> GeneratePdfAsync(PaymentScheduleData data, CancellationToken cancellationToken = default);
}

public sealed record PaymentScheduleData(
    string OrganizationName,
    string? OrganizationLogoUrl,
    string LoanNumber,
    string CustomerName,
    string CustomerDocument,
    string? CustomerPhone,
    string ProductName,
    decimal PrincipalAmount,
    decimal InterestRate,
    string InterestType,
    string PaymentFrequency,
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
    string LoanStatus,
    IReadOnlyList<PaymentScheduleItemData> Items);

public sealed record PaymentScheduleItemData(
    int PaymentNumber,
    DateOnly DueDate,
    decimal? PrincipalAmount,
    decimal? InterestAmount,
    decimal DueAmount,
    decimal PaidAmount,
    decimal PendingAmount,
    decimal? RemainingBalance,
    bool IsPaid,
    bool IsOverdue,
    bool IsCancelled);
