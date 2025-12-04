using FluentAssertions;
using Xunit;
using Alphiq.Domain.Entities;
using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;
using Alphiq.TradingEngine.Risk;
using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Tests.Risk;

public class RiskPercentPositionSizingTests
{
    [Fact]
    public void Constructor_ValidParameters_ShouldCreateInstance()
    {
        var strategy = new RiskPercentPositionSizing(1.0, 10.0);

        strategy.RiskPercent.Should().Be(1.0);
        strategy.PipValue.Should().Be(10.0);
    }

    [Fact]
    public void Constructor_DefaultPipValue_ShouldUseTen()
    {
        var strategy = new RiskPercentPositionSizing(2.0);

        strategy.RiskPercent.Should().Be(2.0);
        strategy.PipValue.Should().Be(10.0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(150)]
    public void Constructor_InvalidRiskPercent_ShouldThrow(double riskPercent)
    {
        var act = () => new RiskPercentPositionSizing(riskPercent);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("riskPercent");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Constructor_InvalidPipValue_ShouldThrow(double pipValue)
    {
        var act = () => new RiskPercentPositionSizing(1.0, pipValue);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("pipValue");
    }

    [Fact]
    public void CalculateVolume_StandardScenario_ShouldCalculateCorrectly()
    {
        // Risk 1% of $10,000 = $100
        // Stop loss = 20 pips, pip value = $10
        // Volume = $100 / (20 * $10) = 0.5 lots
        var strategy = new RiskPercentPositionSizing(1.0, 10.0);
        var context = CreateSignalContext(accountBalance: 10000m);

        var result = strategy.CalculateVolume(context, stopLossPips: 20.0);

        result.Should().Be(0.5);
    }

    [Theory]
    [InlineData(10000, 1.0, 20, 10, 0.5)]   // $100 risk / (20 pips * $10) = 0.5
    [InlineData(10000, 2.0, 20, 10, 1.0)]   // $200 risk / (20 pips * $10) = 1.0
    [InlineData(50000, 1.0, 25, 10, 2.0)]   // $500 risk / (25 pips * $10) = 2.0
    [InlineData(5000, 2.0, 10, 10, 1.0)]    // $100 risk / (10 pips * $10) = 1.0
    [InlineData(10000, 0.5, 10, 10, 0.5)]   // $50 risk / (10 pips * $10) = 0.5
    public void CalculateVolume_VariousScenarios_ShouldCalculateCorrectly(
        decimal balance, double riskPercent, double stopLoss, double pipValue, double expectedVolume)
    {
        var strategy = new RiskPercentPositionSizing(riskPercent, pipValue);
        var context = CreateSignalContext(accountBalance: balance);

        var result = strategy.CalculateVolume(context, stopLossPips: stopLoss);

        result.Should().Be(expectedVolume);
    }

    [Fact]
    public void CalculateVolume_VerySmallResult_ShouldReturnMinimumLot()
    {
        // Risk 0.1% of $100 = $0.10
        // Stop loss = 100 pips, pip value = $10
        // Volume = $0.10 / (100 * $10) = 0.0001 lots -> rounded to 0.01
        var strategy = new RiskPercentPositionSizing(0.1, 10.0);
        var context = CreateSignalContext(accountBalance: 100m);

        var result = strategy.CalculateVolume(context, stopLossPips: 100.0);

        result.Should().Be(0.01);
    }

    [Fact]
    public void CalculateVolume_ShouldRoundToTwoDecimals()
    {
        // Risk 1% of $10,000 = $100
        // Stop loss = 33 pips, pip value = $10
        // Volume = $100 / (33 * $10) = 0.303030... -> 0.30
        var strategy = new RiskPercentPositionSizing(1.0, 10.0);
        var context = CreateSignalContext(accountBalance: 10000m);

        var result = strategy.CalculateVolume(context, stopLossPips: 33.0);

        result.Should().Be(0.30);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void CalculateVolume_InvalidStopLoss_ShouldThrow(double stopLoss)
    {
        var strategy = new RiskPercentPositionSizing(1.0, 10.0);
        var context = CreateSignalContext();

        var act = () => strategy.CalculateVolume(context, stopLossPips: stopLoss);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("stopLossPips");
    }

    [Fact]
    public void CalculateVolume_LargerAccountBalance_ShouldIncreaseLotSize()
    {
        var strategy = new RiskPercentPositionSizing(1.0, 10.0);
        var smallContext = CreateSignalContext(accountBalance: 10000m);
        var largeContext = CreateSignalContext(accountBalance: 100000m);

        var smallResult = strategy.CalculateVolume(smallContext, stopLossPips: 20.0);
        var largeResult = strategy.CalculateVolume(largeContext, stopLossPips: 20.0);

        largeResult.Should().BeGreaterThan(smallResult);
        smallResult.Should().Be(0.5);   // $100 / (20 * 10)
        largeResult.Should().Be(5.0);   // $1000 / (20 * 10)
    }

    [Fact]
    public void CalculateVolume_LargerStopLoss_ShouldDecreaseLotSize()
    {
        var strategy = new RiskPercentPositionSizing(1.0, 10.0);
        var context = CreateSignalContext(accountBalance: 10000m);

        var smallStopResult = strategy.CalculateVolume(context, stopLossPips: 10.0);
        var largeStopResult = strategy.CalculateVolume(context, stopLossPips: 50.0);

        smallStopResult.Should().BeGreaterThan(largeStopResult);
        smallStopResult.Should().Be(1.0);   // $100 / (10 * 10)
        largeStopResult.Should().Be(0.2);   // $100 / (50 * 10)
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
