using System.Text.Json;
using FluentAssertions;
using Xunit;
using Alphiq.Configuration.Abstractions;
using Alphiq.Domain.Entities;
using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;
using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Tests.Strategies;

public class BuyOnFirstBarStrategyTests
{
    private static readonly SymbolId EurusdSymbolId = new(1);

    [Fact]
    public void Constructor_Default_ShouldUseM5Timeframe()
    {
        var strategy = new BuyOnFirstBarStrategy();

        strategy.Name.Should().Be("BuyOnFirstBar");
        strategy.Version.Should().Be(1);
        strategy.MainTimeframe.Should().Be(Timeframe.M5);
        strategy.RequiredTimeframes.Should().ContainKey(Timeframe.M5);
        strategy.RequiredTimeframes[Timeframe.M5].Should().Be(1);
    }

    [Fact]
    public void Constructor_WithTimeframe_ShouldUseProvidedTimeframe()
    {
        var strategy = new BuyOnFirstBarStrategy(Timeframe.H1);

        strategy.MainTimeframe.Should().Be(Timeframe.H1);
        strategy.RequiredTimeframes.Should().ContainKey(Timeframe.H1);
    }

    [Fact]
    public void Constructor_WithDefinition_ShouldUseDefinitionValues()
    {
        var emptyParams = new Dictionary<string, JsonElement>();
        var definition = new StrategyDefinition
        {
            Name = "CustomBuyOnFirstBar",
            Version = 2,
            MainTimeframe = Timeframe.H4,
            Enabled = true,
            RequiredTimeframes = new Dictionary<Timeframe, int>
            {
                { Timeframe.H4, 5 }
            },
            Parameters = emptyParams,
            Symbols = new List<SymbolId> { new(1) },
            Risk = new RiskConfig
            {
                StopLoss = new StopLossConfig { Type = "Fixed", Parameters = emptyParams },
                TakeProfit = new TakeProfitConfig { Type = "Fixed", Parameters = emptyParams },
                PositionSizing = new PositionSizingConfig { Type = "Fixed", Parameters = emptyParams }
            }
        };

        var strategy = new BuyOnFirstBarStrategy(definition);

        strategy.Name.Should().Be("CustomBuyOnFirstBar");
        strategy.Version.Should().Be(2);
        strategy.MainTimeframe.Should().Be(Timeframe.H4);
        strategy.RequiredTimeframes[Timeframe.H4].Should().Be(5);
    }

    [Fact]
    public void Evaluate_FirstBar_ShouldReturnBuySignal()
    {
        var strategy = new BuyOnFirstBarStrategy(Timeframe.M5);
        var context = CreateContext(Timeframe.M5);

        var result = strategy.Evaluate(context);

        result.Signal.Should().Be(TradeSignal.Buy);
        result.SuggestedStopLossPips.Should().Be(10.0);
        result.SuggestedTakeProfitPips.Should().Be(20.0);
        result.SuggestedVolume.Should().Be(0.01);
        result.Reason.Should().Contain("First bar");
    }

    [Fact]
    public void Evaluate_SecondBar_ShouldReturnNoSignal()
    {
        var strategy = new BuyOnFirstBarStrategy(Timeframe.M5);
        var context = CreateContext(Timeframe.M5);

        // First evaluation triggers
        var first = strategy.Evaluate(context);
        // Second evaluation should not trigger
        var second = strategy.Evaluate(context);

        first.Signal.Should().Be(TradeSignal.Buy);
        second.Signal.Should().Be(TradeSignal.None);
    }

    [Fact]
    public void Evaluate_NoMarketData_ShouldReturnNoSignal()
    {
        var strategy = new BuyOnFirstBarStrategy(Timeframe.M5);
        var context = new SignalContext
        {
            SymbolId = EurusdSymbolId,
            Symbol = "EURUSD",
            MarketData = new Dictionary<Timeframe, IReadOnlyList<Bar>>(), // Empty
            AccountBalance = 10000m,
            Timestamp = DateTimeOffset.UtcNow
        };

        var result = strategy.Evaluate(context);

        result.Signal.Should().Be(TradeSignal.None);
    }

    [Fact]
    public void Evaluate_WrongTimeframeData_ShouldReturnNoSignal()
    {
        var strategy = new BuyOnFirstBarStrategy(Timeframe.M5);
        var context = CreateContext(Timeframe.H1); // Different timeframe

        var result = strategy.Evaluate(context);

        result.Signal.Should().Be(TradeSignal.None);
    }

    [Fact]
    public void HasFired_InitialState_ShouldBeFalse()
    {
        var strategy = new BuyOnFirstBarStrategy();

        strategy.HasFired.Should().BeFalse();
    }

    [Fact]
    public void HasFired_AfterEvaluate_ShouldBeTrue()
    {
        var strategy = new BuyOnFirstBarStrategy(Timeframe.M5);
        var context = CreateContext(Timeframe.M5);

        strategy.Evaluate(context);

        strategy.HasFired.Should().BeTrue();
    }

    [Fact]
    public void Reset_AfterFiring_ShouldAllowFiringAgain()
    {
        var strategy = new BuyOnFirstBarStrategy(Timeframe.M5);
        var context = CreateContext(Timeframe.M5);

        // Fire once
        var first = strategy.Evaluate(context);
        first.Signal.Should().Be(TradeSignal.Buy);
        strategy.HasFired.Should().BeTrue();

        // Reset
        strategy.Reset();

        // Should be able to fire again
        strategy.HasFired.Should().BeFalse();
        var second = strategy.Evaluate(context);
        second.Signal.Should().Be(TradeSignal.Buy);
    }

    [Fact]
    public void SignalResult_NoSignal_ShouldHaveNoneSignal()
    {
        var result = SignalResult.NoSignal();

        result.Signal.Should().Be(TradeSignal.None);
        result.SuggestedVolume.Should().BeNull();
        result.Reason.Should().BeNull();
    }

    private static SignalContext CreateContext(Timeframe timeframe, int barCount = 1)
    {
        var bars = Enumerable.Range(0, barCount)
            .Select(i => new Bar
            {
                SymbolId = EurusdSymbolId,
                Timeframe = timeframe,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (i * 300),
                Open = 1.1000,
                High = 1.1050,
                Low = 1.0950,
                Close = 1.1025,
                Volume = 1000
            })
            .ToList();

        return new SignalContext
        {
            SymbolId = EurusdSymbolId,
            Symbol = "EURUSD",
            MarketData = new Dictionary<Timeframe, IReadOnlyList<Bar>>
            {
                { timeframe, bars }
            },
            AccountBalance = 10000m,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
