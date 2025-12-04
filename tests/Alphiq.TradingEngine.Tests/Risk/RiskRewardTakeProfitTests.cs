using FluentAssertions;
using Xunit;
using Alphiq.Domain.Entities;
using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;
using Alphiq.TradingEngine.Risk;
using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Tests.Risk;

public class RiskRewardTakeProfitTests
{
    [Fact]
    public void Constructor_ValidRatio_ShouldCreateInstance()
    {
        var strategy = new RiskRewardTakeProfit(2.0);

        strategy.RiskRewardRatio.Should().Be(2.0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    public void Constructor_InvalidRatio_ShouldThrow(double ratio)
    {
        var act = () => new RiskRewardTakeProfit(ratio);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("riskRewardRatio");
    }

    [Fact]
    public void CalculateTakeProfitPips_WithRatio2_ShouldReturnDoubleStopLoss()
    {
        var strategy = new RiskRewardTakeProfit(2.0);
        var context = CreateSignalContext();

        var result = strategy.CalculateTakeProfitPips(context, stopLossPips: 20.0);

        result.Should().Be(40.0);
    }

    [Theory]
    [InlineData(1.0, 10.0, 10.0)]
    [InlineData(1.5, 20.0, 30.0)]
    [InlineData(2.0, 15.0, 30.0)]
    [InlineData(3.0, 10.0, 30.0)]
    [InlineData(0.5, 40.0, 20.0)]
    public void CalculateTakeProfitPips_VariousRatios_ShouldCalculateCorrectly(
        double ratio, double stopLoss, double expectedTakeProfit)
    {
        var strategy = new RiskRewardTakeProfit(ratio);
        var context = CreateSignalContext();

        var result = strategy.CalculateTakeProfitPips(context, stopLossPips: stopLoss);

        result.Should().Be(expectedTakeProfit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void CalculateTakeProfitPips_InvalidStopLoss_ShouldThrow(double stopLoss)
    {
        var strategy = new RiskRewardTakeProfit(2.0);
        var context = CreateSignalContext();

        var act = () => strategy.CalculateTakeProfitPips(context, stopLossPips: stopLoss);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("stopLossPips");
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
