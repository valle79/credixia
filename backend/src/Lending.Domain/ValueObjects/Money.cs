using Lending.Domain.Common;

namespace Lending.Domain.ValueObjects;

/// <summary>
/// Immutable monetary amount. Every operation rounds to the currency precision using
/// HALF_UP (away from zero), the policy approved for this product.
/// </summary>
public sealed record Money
{
    public const MidpointRounding RoundingMode = MidpointRounding.AwayFromZero;

    private Money(decimal amount, Currency currency)
    {
        Amount = decimal.Round(amount, currency.DecimalPlaces, RoundingMode);
        Currency = currency;
    }

    public decimal Amount { get; }

    public Currency Currency { get; }

    public bool IsZero => Amount == 0m;

    public bool IsNegative => Amount < 0m;

    public static Money Of(decimal amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return new Money(amount, currency);
    }

    public static Money Of(decimal amount, string currencyCode) =>
        new(amount, Currency.FromCode(currencyCode));

    public static Money Zero(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return new Money(0m, currency);
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    public Money Divide(decimal divisor)
    {
        if (divisor == 0m)
        {
            throw new DivideByZeroException("Money cannot be divided by zero.");
        }

        return new Money(Amount / divisor, Currency);
    }

    public Money Negate() => new(-Amount, Currency);

    public Money Abs() => new(Math.Abs(Amount), Currency);

    public override string ToString() =>
        $"{Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} {Currency.Code}";

    private void EnsureSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Currency != other.Currency)
        {
            throw new DomainException(
                "money.currency_mismatch",
                $"Cannot operate {Currency.Code} with {other.Currency.Code}.");
        }
    }
}
