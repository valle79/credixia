using Lending.Domain.Common;
using Lending.Domain.LoanProducts;
using Lending.Domain.ValueObjects;

namespace Lending.Domain.Loans;

/// <summary>
/// Préstamo otorgado a un cliente
/// </summary>
public sealed class Loan : Entity
{
    private readonly List<PaymentScheduleItem> _schedule = [];
    private readonly List<Payment> _payments = [];

    private Loan()
    {
    }

    private Loan(
        Guid tenantId,
        Guid customerId,
        Guid loanProductId,
        string loanNumber,
        Money principalAmount,
        decimal interestRate,
        InterestType interestType,
        PaymentFrequency frequency,
        int termDays,
        int numberOfPayments,
        DateOnly disbursementDate,
        DateOnly firstPaymentDate,
        Money? commission,
        DateTimeOffset now)
    {
        TenantId = tenantId;
        CustomerId = customerId;
        LoanProductId = loanProductId;
        LoanNumber = loanNumber;
        PrincipalAmount = principalAmount ?? throw new ArgumentNullException(nameof(principalAmount));
        InterestRate = interestRate;
        InterestType = interestType;
        Frequency = frequency;
        TermDays = termDays;
        NumberOfPayments = numberOfPayments;
        DisbursementDate = disbursementDate;
        FirstPaymentDate = firstPaymentDate;
        Commission = commission;
        Status = LoanStatus.Pending;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid TenantId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid LoanProductId { get; private set; }

    public string LoanNumber { get; private set; } = string.Empty;

    public Money PrincipalAmount { get; private set; } = null!;

    public decimal InterestRate { get; private set; }

    public InterestType InterestType { get; private set; }

    public PaymentFrequency Frequency { get; private set; }

    public int TermDays { get; private set; }

    public int NumberOfPayments { get; private set; }

    public DateOnly DisbursementDate { get; private set; }

    public DateOnly FirstPaymentDate { get; private set; }

    public Money? Commission { get; private set; }

    public Money TotalInterest { get; private set; } = null!;

    public Money TotalAmount { get; private set; } = null!;

    public Money PaidAmount { get; private set; } = null!;

    public Money BalanceAmount { get; private set; } = null!;

    public LoanStatus Status { get; private set; }

    public DateOnly? ApprovedDate { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }

    public DateOnly? DisbursedDate { get; private set; }

    public Guid? DisbursedByUserId { get; private set; }

    public DateOnly? ClosedDate { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<PaymentScheduleItem> Schedule => _schedule;

    public IReadOnlyCollection<Payment> Payments => _payments;

    public static Loan Create(
        Guid tenantId,
        Guid customerId,
        Guid loanProductId,
        string loanNumber,
        Money principalAmount,
        decimal interestRate,
        InterestType interestType,
        PaymentFrequency frequency,
        int termDays,
        int numberOfPayments,
        DateOnly disbursementDate,
        DateOnly firstPaymentDate,
        Money? commission,
        DateTimeOffset now)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer identifier cannot be empty.", nameof(customerId));
        }

        if (loanProductId == Guid.Empty)
        {
            throw new ArgumentException("Loan product identifier cannot be empty.", nameof(loanProductId));
        }

        if (string.IsNullOrWhiteSpace(loanNumber))
        {
            throw new DomainException("loan.number.required", "El número de préstamo es requerido.");
        }

        if (numberOfPayments <= 0)
        {
            throw new DomainException("loan.payments.invalid", "El número de cuotas debe ser mayor a cero.");
        }

        EnsureTimestamp(now);
        return new Loan(
            tenantId,
            customerId,
            loanProductId,
            loanNumber.Trim().ToUpperInvariant(),
            principalAmount,
            interestRate,
            interestType,
            frequency,
            termDays,
            numberOfPayments,
            disbursementDate,
            firstPaymentDate,
            commission,
            now);
    }

    public void GenerateSchedule(DateTimeOffset now)
    {
        EnsureTimestamp(now);

        if (Status != LoanStatus.Pending)
        {
            throw new DomainException("loan.schedule.invalid_status", "Solo se puede generar cronograma para préstamos pendientes.");
        }

        _schedule.Clear();

        var paymentAmount = CalculatePaymentAmount();
        var currentDate = FirstPaymentDate;
        var remainingPrincipal = PrincipalAmount.Amount;

        for (int i = 1; i <= NumberOfPayments; i++)
        {
            // Calcular interés y capital para esta cuota
            Money interestAmount;
            Money principalAmount;
            
            if (InterestType == InterestType.Simple)
            {
                // En interés simple, el interés total se divide equitativamente
                var totalInterest = PrincipalAmount.Amount * (InterestRate / 100);
                interestAmount = Money.Of(totalInterest / NumberOfPayments, PrincipalAmount.Currency);
                principalAmount = Money.Of(paymentAmount.Amount - interestAmount.Amount, PrincipalAmount.Currency);
            }
            else
            {
                // En interés compuesto, el interés se calcula sobre el saldo pendiente
                var interestForPeriod = remainingPrincipal * ((InterestRate / 100) / GetPeriodsPerYear(Frequency));
                interestAmount = Money.Of(interestForPeriod, PrincipalAmount.Currency);
                principalAmount = Money.Of(paymentAmount.Amount - interestAmount.Amount, PrincipalAmount.Currency);
            }

            // Actualizar saldo pendiente
            remainingPrincipal -= principalAmount.Amount;
            
            // Asegurar que el saldo no sea negativo por redondeos
            if (i == NumberOfPayments)
            {
                remainingPrincipal = 0;
            }

            var item = new PaymentScheduleItem(
                Id,
                i,
                currentDate,
                Money.Of(paymentAmount.Amount, paymentAmount.Currency),
                Money.Of(0m, PrincipalAmount.Currency),
                Money.Of(0m, PrincipalAmount.Currency),
                principalAmount,
                interestAmount,
                Money.Of(Math.Max(0, remainingPrincipal), PrincipalAmount.Currency));

            _schedule.Add(item);
            currentDate = AddPaymentPeriod(currentDate, Frequency);
        }

        // Calcular interés total
        TotalInterest = paymentAmount.Multiply(NumberOfPayments).Subtract(PrincipalAmount);
        
        if (Commission is not null)
        {
            TotalAmount = PrincipalAmount.Add(TotalInterest).Add(Commission);
        }
        else
        {
            TotalAmount = PrincipalAmount.Add(TotalInterest);
        }

        PaidAmount = Money.Zero(PrincipalAmount.Currency);
        BalanceAmount = Money.Of(TotalAmount.Amount, TotalAmount.Currency);
        UpdatedAt = now;
    }

    public void Approve(Guid approvedByUserId, DateTimeOffset now)
    {
        EnsureTimestamp(now);

        if (Status != LoanStatus.Pending)
        {
            throw new DomainException("loan.approve.invalid_status", "Solo se pueden aprobar préstamos pendientes.");
        }

        Status = LoanStatus.Approved;
        ApprovedDate = DateOnly.FromDateTime(now.Date);
        ApprovedByUserId = approvedByUserId;
        UpdatedAt = now;
    }

    public void Reject(DateTimeOffset now)
    {
        EnsureTimestamp(now);

        if (Status != LoanStatus.Pending)
        {
            throw new DomainException("loan.reject.invalid_status", "Solo se pueden rechazar préstamos pendientes.");
        }

        Status = LoanStatus.Rejected;
        UpdatedAt = now;
    }

    public void Disburse(Guid disbursedByUserId, DateTimeOffset now)
    {
        EnsureTimestamp(now);

        if (Status != LoanStatus.Approved)
        {
            throw new DomainException("loan.disburse.invalid_status", "Solo se pueden desembolsar préstamos aprobados.");
        }

        Status = LoanStatus.Active;
        DisbursedDate = DateOnly.FromDateTime(now.Date);
        DisbursedByUserId = disbursedByUserId;
        UpdatedAt = now;
    }

    public Payment RegisterPayment(
        string receiptNumber,
        Money amount,
        DateOnly paymentDate,
        Guid receivedByUserId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(amount);
        EnsureTimestamp(now);

        if (Status != LoanStatus.Active)
        {
            throw new DomainException("loan.payment.invalid_status", "Solo se pueden registrar pagos en préstamos activos.");
        }

        if (amount.Amount <= 0)
        {
            throw new DomainException("loan.payment.amount.invalid", "El monto del pago debe ser mayor a cero.");
        }

        var payment = new Payment(
            Id,
            receiptNumber,
            amount,
            paymentDate,
            receivedByUserId,
            now);

        _payments.Add(payment);

        // Actualizar saldo
        PaidAmount = PaidAmount.Add(amount);
        BalanceAmount = TotalAmount.Subtract(PaidAmount);

        // Aplicar pago al cronograma
        ApplyPaymentToSchedule(amount, paymentDate);

        // Si el saldo es cero, cerrar el préstamo
        if (BalanceAmount.Amount <= 0)
        {
            Status = LoanStatus.PaidOff;
            ClosedDate = paymentDate;
        }

        UpdatedAt = now;

        return payment;
    }

    public void SetNotes(string? notes, DateTimeOffset now)
    {
        EnsureTimestamp(now);
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdatedAt = now;
    }

    private Money CalculatePaymentAmount()
    {
        if (InterestType == InterestType.Simple)
        {
            // Interés simple: se aplica la tasa sobre el capital total
            // Total a pagar = Capital + (Capital × Tasa)
            var totalInterest = PrincipalAmount.Amount * (InterestRate / 100);
            var totalAmount = PrincipalAmount.Amount + totalInterest;
            var paymentAmount = totalAmount / NumberOfPayments;
            return Money.Of(paymentAmount, PrincipalAmount.Currency);
        }
        else
        {
            // Interés compuesto - fórmula de amortización
            var r = (InterestRate / 100) / GetPeriodsPerYear(Frequency);
            var n = NumberOfPayments;
            var p = PrincipalAmount.Amount;
            
            var paymentAmount = p * (r * (decimal)Math.Pow((double)(1 + r), n)) / 
                               ((decimal)Math.Pow((double)(1 + r), n) - 1);
            
            return Money.Of(paymentAmount, PrincipalAmount.Currency);
        }
    }

    private void ApplyPaymentToSchedule(Money amount, DateOnly paymentDate)
    {
        var remainingAmount = amount.Amount;

        foreach (var item in _schedule.OrderBy(s => s.PaymentNumber))
        {
            if (remainingAmount <= 0)
            {
                break;
            }

            var pendingAmount = item.DueAmount.Amount - item.PaidAmount.Amount;
            if (pendingAmount <= 0)
            {
                continue;
            }

            var paymentForItem = Math.Min(remainingAmount, pendingAmount);
            item.AddPayment(Money.Of(paymentForItem, amount.Currency), paymentDate);
            remainingAmount -= paymentForItem;
        }
    }

    private static DateOnly AddPaymentPeriod(DateOnly date, PaymentFrequency frequency)
    {
        return frequency switch
        {
            PaymentFrequency.Daily => date.AddDays(1),
            PaymentFrequency.Weekly => date.AddDays(7),
            PaymentFrequency.Biweekly => date.AddDays(15),
            PaymentFrequency.Monthly => date.AddMonths(1),
            PaymentFrequency.Quarterly => date.AddMonths(3),
            PaymentFrequency.Biannual => date.AddMonths(6),
            PaymentFrequency.Annual => date.AddYears(1),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency))
        };
    }

    private static int GetPeriodsPerYear(PaymentFrequency frequency)
    {
        return frequency switch
        {
            PaymentFrequency.Daily => 365,
            PaymentFrequency.Weekly => 52,
            PaymentFrequency.Biweekly => 24,
            PaymentFrequency.Monthly => 12,
            PaymentFrequency.Quarterly => 4,
            PaymentFrequency.Biannual => 2,
            PaymentFrequency.Annual => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(frequency))
        };
    }

    private static void EnsureTimestamp(DateTimeOffset now)
    {
        if (now == default)
        {
            throw new ArgumentException("A valid timestamp is required.", nameof(now));
        }
    }
}
