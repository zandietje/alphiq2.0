using Alphiq.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Alphiq.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Adding_SameCurrency_ShouldSucceed()
    {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "USD");

        var result = a + b;

        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Adding_DifferentCurrency_ShouldThrow()
    {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "EUR");

        var act = () => a + b;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtracting_SameCurrency_ShouldSucceed()
    {
        var a = new Money(100m, "USD");
        var b = new Money(30m, "USD");

        var result = a - b;

        result.Amount.Should().Be(70m);
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        var money = new Money(1234.56m, "EUR");

        money.ToString().Should().Be("1,234.56 EUR");
    }
}
