using Lending.Domain.Common;

namespace Lending.Domain.ValueObjects;

/// <summary>
/// ISO-4217 currency with the number of decimal places used for monetary rounding.
/// </summary>
public sealed record Currency
{
    public static readonly Currency Pen = new("PEN", 2);
    public static readonly Currency Usd = new("USD", 2);

    private static readonly IReadOnlyDictionary<string, Currency> _registry =
        new Dictionary<string, Currency>(StringComparer.OrdinalIgnoreCase)
        {
            [Pen.Code] = Pen,
            [Usd.Code] = Usd,
        };

    private Currency(string code, int decimalPlaces)
    {
        Code = code;
        DecimalPlaces = decimalPlaces;
    }

    public string Code { get; }

    public int DecimalPlaces { get; }

    public static IReadOnlyCollection<Currency> Supported => _registry.Values.ToArray();

    public static Currency FromCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || !_registry.TryGetValue(code.Trim(), out var currency))
        {
            throw new DomainException(
                "money.currency.unsupported",
                $"Currency '{code}' is not supported.");
        }

        return currency;
    }

    public override string ToString() => Code;
}

