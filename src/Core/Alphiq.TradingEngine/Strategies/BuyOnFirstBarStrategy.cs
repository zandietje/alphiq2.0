using Alphiq.Configuration.Abstractions;
using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.TradingEngine.Strategies;

/// <summary>
/// Trivial test strategy that produces a Buy signal on the first bar only.
/// Used for testing the engine flow end-to-end.
/// </summary>
public sealed class BuyOnFirstBarStrategy : ISignalStrategy
{
    private bool _hasFired = false;

    public string Name { get; }
    public int Version { get; }
    public Timeframe MainTimeframe { get; }
    public IReadOnlyDictionary<Timeframe, int> RequiredTimeframes { get; }

    /// <summary>
    /// Creates a BuyOnFirstBarStrategy with default settings (M5 timeframe).
    /// </summary>
    public BuyOnFirstBarStrategy() : this(null)
    {
    }

    /// <summary>
    /// Creates a BuyOnFirstBarStrategy from a strategy definition.
    /// </summary>
    public BuyOnFirstBarStrategy(StrategyDefinition? definition)
    {
        Name = definition?.Name ?? "BuyOnFirstBar";
        Version = definition?.Version ?? 1;
        MainTimeframe = definition?.MainTimeframe ?? Timeframe.M5;
        RequiredTimeframes = definition?.RequiredTimeframes ??
            new Dictionary<Timeframe, int> { { MainTimeframe, 1 } };
    }

    /// <summary>
    /// Creates a BuyOnFirstBarStrategy with a specific timeframe.
    /// </summary>
    public BuyOnFirstBarStrategy(Timeframe mainTimeframe)
    {
        Name = "BuyOnFirstBar";
        Version = 1;
        MainTimeframe = mainTimeframe;
        RequiredTimeframes = new Dictionary<Timeframe, int> { { mainTimeframe, 1 } };
    }

    public SignalResult Evaluate(SignalContext context)
    {
        // Only fire once
        if (_hasFired)
        {
            return SignalResult.NoSignal();
        }

        // Verify we have the required market data
        if (!context.MarketData.TryGetValue(MainTimeframe, out var bars) || bars.Count == 0)
        {
            return SignalResult.NoSignal();
        }

        _hasFired = true;

        return new SignalResult
        {
            Signal = TradeSignal.Buy,
            SuggestedStopLossPips = 10.0,
            SuggestedTakeProfitPips = 20.0,
            SuggestedVolume = 0.01,
            Reason = "First bar - test signal"
        };
    }

    /// <summary>
    /// Resets the strategy state so it can fire again.
    /// Useful for testing.
    /// </summary>
    public void Reset()
    {
        _hasFired = false;
    }

    /// <summary>
    /// Indicates whether the strategy has already fired.
    /// </summary>
    public bool HasFired => _hasFired;
}
