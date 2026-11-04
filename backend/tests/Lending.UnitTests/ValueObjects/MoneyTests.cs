using Lending.Domain.Common;
using Lending.Domain.ValueObjects;

namespace Lending.UnitTests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Of_rounds_half_away_from_zero()
    {
        Assert.Equal(10.01m, Money.Of(10.005m, Currency.Pen).Amount);
        Assert.Equal(10.00m, Money.Of(10.004m, Currency.Pen).Amount);
        Assert.Equal(-10.01m, Money.Of(-10.005m, Currency.Pen).Amount);
    }

    [Fact]
    public void Arithmetic_preserves_currency_and_precision()
    {
        var a = Money.Of(100.10m, Currency.Pen);
        var b = Money.Of(0.20m, Currency.Pen);

        Assert.Equal(100.30m, a.Add(b).Amount);
        Assert.Equal(99.90m, a.Subtract(b).Amount);
        Assert.Equal(200.20m, a.Multiply(2m).Amount);
        Assert.Equal(50.05m, a.Divide(2m).Amount);
        Assert.Equal(Currency.Pen, a.Add(b).Currency);
    }

    [Fact]
    public void Add_with_different_currencies_throws_domain_error()
    {
        var soles = Money.Of(10m, Currency.Pen);
        var dollars = Money.Of(10m, Currency.Usd);

        var exception = Assert.Throws<DomainException>(() => soles.Add(dollars));

        Assert.Equal("money.currency_mismatch", exception.Code);
    }

    [Fact]
    public void Divide_by_zero_throws()
    {
        var money = Money.Of(10m, Currency.Pen);

        Assert.Throws<DivideByZeroException>(() => money.Divide(0m));
    }

    [Fact]
    public void Of_with_string_code_is_case_insensitive()
    {
        Assert.Same(Currency.Pen, Money.Of(5m, " pen ").Currency);
    }

    [Fact]
    public void Of_with_unsupported_currency_throws_domain_error()
    {
        var exception = Assert.Throws<DomainException>(() => Money.Of(5m, "EUR"));

        Assert.Equal("money.currency.unsupported", exception.Code);
    }

    [Fact]
    public void ToString_uses_invariant_two_decimals()
    {
        Assert.Equal("1234.50 PEN", Money.Of(1234.5m, Currency.Pen).ToString());
    }
}
