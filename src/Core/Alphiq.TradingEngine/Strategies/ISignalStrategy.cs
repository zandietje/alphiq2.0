using Alphiq.Domain.Entities;
using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.TradingEngine.Strategies;

/// <summary>
/// Signal strategy interface - generates trading signals from market data.
/// </summary>
public interface ISignalStrategy
{
    string Name { get; }
    int Version { get; }
    Timeframe MainTimeframe { get; }
    IReadOnlyDictionary<Timeframe, int> RequiredTimeframes { get; }

    /// <summary>
    /// Evaluates market data and returns a signal.
    /// </summary>
    SignalResult Evaluate(SignalContext context);
}

/// <summary>
/// Context provided to strategy for evaluation.
/// </summary>
public sealed record SignalContext
{
    public required SymbolId SymbolId { get; init; }
    public required string Symbol { get; init; }
    public required IReadOnlyDictionary<Timeframe, IReadOnlyList<Bar>> MarketData { get; init; }
    public required decimal AccountBalance { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Result of strategy evaluation.
/// </summary>
public sealed record SignalResult
{
    public TradeSignal Signal { get; init; } = TradeSignal.None;
    public double? SuggestedStopLossPips { get; init; }
    public double? SuggestedTakeProfitPips { get; init; }
    public double? SuggestedVolume { get; init; }
    public string? Reason { get; init; }

    public static SignalResult NoSignal() => new();
    public static SignalResult Buy(string? reason = null) => new() { Signal = TradeSignal.Buy, Reason = reason };
    public static SignalResult Sell(string? reason = null) => new() { Signal = TradeSignal.Sell, Reason = reason };
}
