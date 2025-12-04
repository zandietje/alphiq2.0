using FluentAssertions;
using Xunit;
using Alphiq.Domain.Entities;
using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;
using Alphiq.TradingEngine.Risk;
using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Tests.Risk;

public class FixedPipsStopLossTests
{
    [Fact]
    public void Constructor_ValidPips_ShouldCreateInstance()
    {
        var strategy = new FixedPipsStopLoss(20.0);

        strategy.Pips.Should().Be(20.0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_InvalidPips_ShouldThrow(double pips)
    {
        var act = () => new FixedPipsStopLoss(pips);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("pips");
    }

    [Fact]
    public void CalculateStopLossPips_ShouldReturnFixedValue()
    {
        var strategy = new FixedPipsStopLoss(15.5);
        var context = CreateSignalContext();

        var result = strategy.CalculateStopLossPips(context);

        result.Should().Be(15.5);
    }

    [Fact]
    public void CalculateStopLossPips_DifferentContexts_ShouldReturnSameValue()
    {
        var strategy = new FixedPipsStopLoss(25.0);
        var context1 = CreateSignalContext(accountBalance: 1000m);
        var context2 = CreateSignalContext(accountBalance: 100000m);

        var result1 = strategy.CalculateStopLossPips(context1);
        var result2 = strategy.CalculateStopLossPips(context2);

        result1.Should().Be(25.0);
        result2.Should().Be(25.0);
    }

    private static SignalContext CreateSignalContext(decimal accountBalance = 10000m)
    {
        return new SignalContext
        {
            SymbolId = new SymbolId(1),
            Symbol = "EURUSD",
            MarketData = new Dictionary<Timeframe, IReadOnlyList<Bar>>(),
            AccountBalance = accountBalance,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
