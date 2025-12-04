using FluentAssertions;
using Xunit;
using Alphiq.Domain.Entities;
using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;
using Alphiq.TradingEngine.Risk;
using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Tests.Risk;

public class FixedLotPositionSizingTests
{
    [Fact]
    public void Constructor_ValidLots_ShouldCreateInstance()
    {
        var strategy = new FixedLotPositionSizing(0.1);

        strategy.Lots.Should().Be(0.1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-1)]
    public void Constructor_InvalidLots_ShouldThrow(double lots)
    {
        var act = () => new FixedLotPositionSizing(lots);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("lots");
    }

    [Fact]
    public void CalculateVolume_ShouldReturnFixedValue()
    {
        var strategy = new FixedLotPositionSizing(0.05);
        var context = CreateSignalContext();

        var result = strategy.CalculateVolume(context, stopLossPips: 20.0);

        result.Should().Be(0.05);
    }

    [Fact]
    public void CalculateVolume_IgnoresStopLoss_ShouldReturnFixedValue()
    {
        var strategy = new FixedLotPositionSizing(0.1);
        var context = CreateSignalContext();

        var result1 = strategy.CalculateVolume(context, stopLossPips: 10.0);
        var result2 = strategy.CalculateVolume(context, stopLossPips: 100.0);

        result1.Should().Be(0.1);
        result2.Should().Be(0.1);
    }

    [Fact]
    public void CalculateVolume_IgnoresAccountBalance_ShouldReturnFixedValue()
    {
        var strategy = new FixedLotPositionSizing(0.25);
        var context1 = CreateSignalContext(accountBalance: 1000m);
        var context2 = CreateSignalContext(accountBalance: 100000m);

        var result1 = strategy.CalculateVolume(context1, stopLossPips: 20.0);
        var result2 = strategy.CalculateVolume(context2, stopLossPips: 20.0);

        result1.Should().Be(0.25);
        result2.Should().Be(0.25);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.1)]
    [InlineData(1.0)]
    [InlineData(10.0)]
    public void CalculateVolume_VariousLotSizes_ShouldReturnConfiguredValue(double lots)
    {
        var strategy = new FixedLotPositionSizing(lots);
        var context = CreateSignalContext();

        var result = strategy.CalculateVolume(context, stopLossPips: 15.0);

        result.Should().Be(lots);
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
