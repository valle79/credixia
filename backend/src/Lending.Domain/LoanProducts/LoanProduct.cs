using Lending.Domain.Common;
using Lending.Domain.ValueObjects;

namespace Lending.Domain.LoanProducts;

/// <summary>
/// Producto de préstamo configurado por el tenant (tipo de préstamo).
/// Define los términos y condiciones bajo los cuales se otorgan préstamos.
/// </summary>
public sealed class LoanProduct : Entity
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 50;

    private LoanProduct()
    {
    }

    private LoanProduct(
        Guid tenantId,
        string code,
        string name,
        PaymentFrequency frequency,
        decimal interestRate,
        InterestType interestType,
        Money minimumAmount,
        Money maximumAmount,
        int minimumTermDays,
        int maximumTermDays,
        DateTimeOffset now)
    {
        TenantId = tenantId;
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        Frequency = frequency;
        InterestRate = ValidateInterestRate(interestRate);
        InterestType = interestType;
        MinimumAmount = minimumAmount ?? throw new ArgumentNullException(nameof(minimumAmount));
        MaximumAmount = maximumAmount ?? throw new ArgumentNullException(nameof(maximumAmount));
        MinimumTermDays = ValidateTermDays(minimumTermDays, nameof(minimumTermDays));
        MaximumTermDays = ValidateTermDays(maximumTermDays, nameof(maximumTermDays));
        IsActive = true;
        CreatedAt = now;
        UpdatedAt = now;

        ValidateAmountRange();
        ValidateTermRange();
    }

    public Guid TenantId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public PaymentFrequency Frequency { get; private set; }

    public decimal InterestRate { get; private set; }

    public InterestType InterestType { get; private set; }

    public Money MinimumAmount { get; private set; } = null!;

    public Money MaximumAmount { get; private set; } = null!;

    public int MinimumTermDays { get; private set; }

    public int MaximumTermDays { get; private set; }

    public decimal? CommissionPercentage { get; private set; }

    public Money? FixedCommission { get; private set; }

    public bool RequiresGuarantor { get; private set; }

    public bool IsActive { get; private set; }

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static LoanProduct Create(
        Guid tenantId,
        string code,
        string name,
        PaymentFrequency frequency,
        decimal interestRate,
        InterestType interestType,
        Money minimumAmount,
        Money maximumAmount,
        int minimumTermDays,
        int maximumTermDays,
        DateTimeOffset now)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        EnsureTimestamp(now);
        return new LoanProduct(
            tenantId,
            code,
            name,
            frequency,
            interestRate,
            interestType,
            minimumAmount,
            maximumAmount,
            minimumTermDays,
            maximumTermDays,
            now);
    }

    public void Update(
        string name,
        decimal interestRate,
        Money minimumAmount,
        Money maximumAmount,
        int minimumTermDays,
        int maximumTermDays,
        decimal? commissionPercentage,
        Money? fixedCommission,
        bool requiresGuarantor,
        string? description,
        DateTimeOffset now)
    {
        EnsureTimestamp(now);

        Name = NormalizeName(name);
        InterestRate = ValidateInterestRate(interestRate);
        MinimumAmount = minimumAmount ?? throw new ArgumentNullException(nameof(minimumAmount));
        MaximumAmount = maximumAmount ?? throw new ArgumentNullException(nameof(maximumAmount));
        MinimumTermDays = ValidateTermDays(minimumTermDays, nameof(minimumTermDays));
        MaximumTermDays = ValidateTermDays(maximumTermDays, nameof(maximumTermDays));
        
        if (commissionPercentage.HasValue && (commissionPercentage.Value < 0 || commissionPercentage.Value > 100))
        {
            throw new DomainException("loan_product.commission.invalid", "El porcentaje de comisión debe estar entre 0 y 100.");
        }

        CommissionPercentage = commissionPercentage;
        FixedCommission = fixedCommission;
        RequiresGuarantor = requiresGuarantor;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAt = now;

        ValidateAmountRange();
        ValidateTermRange();
    }

    public void Activate(DateTimeOffset now)
    {
        EnsureTimestamp(now);
        IsActive = true;
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        EnsureTimestamp(now);
        IsActive = false;
        UpdatedAt = now;
    }

    private void ValidateAmountRange()
    {
        if (MinimumAmount.Amount > MaximumAmount.Amount)
        {
            throw new DomainException(
                "loan_product.amount_range.invalid",
                "El monto mínimo no puede ser mayor que el monto máximo.");
        }
    }

    private void ValidateTermRange()
    {
        if (MinimumTermDays > MaximumTermDays)
        {
            throw new DomainException(
                "loan_product.term_range.invalid",
                "El plazo mínimo no puede ser mayor que el plazo máximo.");
        }
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("loan_product.name.required", "El nombre del producto es requerido.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException(
                "loan_product.name.too_long",
                $"El nombre no puede exceder {NameMaxLength} caracteres.");
        }

        return trimmed;
    }

    private static string NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("loan_product.code.required", "El código del producto es requerido.");
        }

        var trimmed = code.Trim().ToUpperInvariant();
        if (trimmed.Length > CodeMaxLength)
        {
            throw new DomainException(
                "loan_product.code.too_long",
                $"El código no puede exceder {CodeMaxLength} caracteres.");
        }

        return trimmed;
    }

    private static decimal ValidateInterestRate(decimal rate)
    {
        if (rate < 0 || rate > 100)
        {
            throw new DomainException(
                "loan_product.interest_rate.invalid",
                "La tasa de interés debe estar entre 0 y 100.");
        }

        return rate;
    }

    private static int ValidateTermDays(int days, string paramName)
    {
        if (days <= 0)
        {
            throw new DomainException(
                "loan_product.term.invalid",
                "El plazo debe ser mayor a cero.");
        }

        return days;
    }

    private static void EnsureTimestamp(DateTimeOffset now)
    {
        if (now == default)
        {
            throw new ArgumentException("A valid timestamp is required.", nameof(now));
        }
    }
}
