using FluentAssertions;
using Xunit;
using Alphiq.Domain.Entities;
using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;
using Alphiq.TradingEngine.Risk;
using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Tests.Risk;

public class FixedPipsTakeProfitTests
{
    [Fact]
    public void Constructor_ValidPips_ShouldCreateInstance()
    {
        var strategy = new FixedPipsTakeProfit(40.0);

        strategy.Pips.Should().Be(40.0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Constructor_InvalidPips_ShouldThrow(double pips)
    {
        var act = () => new FixedPipsTakeProfit(pips);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("pips");
    }

    [Fact]
    public void CalculateTakeProfitPips_ShouldReturnFixedValue()
    {
        var strategy = new FixedPipsTakeProfit(30.0);
        var context = CreateSignalContext();

        var result = strategy.CalculateTakeProfitPips(context, stopLossPips: 15.0);

        result.Should().Be(30.0);
    }

    [Fact]
    public void CalculateTakeProfitPips_IgnoresStopLoss_ShouldReturnFixedValue()
    {
        var strategy = new FixedPipsTakeProfit(50.0);
        var context = CreateSignalContext();

        var result1 = strategy.CalculateTakeProfitPips(context, stopLossPips: 10.0);
        var result2 = strategy.CalculateTakeProfitPips(context, stopLossPips: 100.0);

        result1.Should().Be(50.0);
        result2.Should().Be(50.0);
    }

    [Fact]
    public void CalculateTakeProfitPips_DifferentContexts_ShouldReturnSameValue()
    {
        var strategy = new FixedPipsTakeProfit(25.0);
        var context1 = CreateSignalContext(accountBalance: 1000m);
        var context2 = CreateSignalContext(accountBalance: 100000m);

        var result1 = strategy.CalculateTakeProfitPips(context1, stopLossPips: 10.0);
        var result2 = strategy.CalculateTakeProfitPips(context2, stopLossPips: 10.0);

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
