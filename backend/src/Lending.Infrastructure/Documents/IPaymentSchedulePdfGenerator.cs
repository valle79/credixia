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
    decimal PrincipalAmount,
    decimal InterestRate,
    string InterestType,
    string PaymentFrequency,
    int NumberOfPayments,
    decimal TotalAmount,
    DateOnly FirstPaymentDate,
    IReadOnlyList<PaymentScheduleItemData> Items);

public sealed record PaymentScheduleItemData(
    int PaymentNumber,
    DateOnly DueDate,
    decimal DueAmount,
    decimal PaidAmount,
    decimal PendingAmount);
