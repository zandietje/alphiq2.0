using Microsoft.Extensions.Logging;
using Alphiq.Brokers.Abstractions;
using Alphiq.Domain.Entities;
using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;
using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Engine;

/// <summary>
/// The single unified trading engine - used by live, backtest, and paper trading.
/// All trading logic flows through this engine.
/// </summary>
public sealed class TradingEngineService
{
    private readonly IMarketDataFeed _marketData;
    private readonly IOrderExecution _orderExecution;
    private readonly IClock _clock;
    private readonly IEngineEventSink _eventSink;
    private readonly ILogger<TradingEngineService> _logger;

    private readonly Dictionary<SymbolId, Dictionary<Timeframe, List<Bar>>> _barCache = new();
    private readonly List<ISignalStrategy> _strategies = new();

    public TradingEngineService(
        IMarketDataFeed marketData,
        IOrderExecution orderExecution,
        IClock clock,
        IEngineEventSink eventSink,
        ILogger<TradingEngineService> logger)
    {
        _marketData = marketData;
        _orderExecution = orderExecution;
        _clock = clock;
        _eventSink = eventSink;
        _logger = logger;
    }

    /// <summary>
    /// Registers a strategy with the engine.
    /// </summary>
    public void RegisterStrategy(ISignalStrategy strategy)
    {
        _strategies.Add(strategy);
        _logger.LogInformation("Registered strategy {Name} v{Version}", strategy.Name, strategy.Version);
    }

    /// <summary>
    /// Processes a new bar and evaluates strategies.
    /// </summary>
    public async Task OnBarClosedAsync(Bar bar, CancellationToken ct = default)
    {
        // Update cache
        UpdateCache(bar);

        // Evaluate strategies that use this timeframe as main
        foreach (var strategy in _strategies.Where(s => s.MainTimeframe == bar.Timeframe))
        {
            var context = BuildContext(bar.SymbolId, strategy);
            if (context is null) continue;

            var result = strategy.Evaluate(context);

            if (result.Signal != TradeSignal.None)
            {
                await ProcessSignalAsync(bar.SymbolId, strategy, result, ct);
            }
        }
    }

    private void UpdateCache(Bar bar)
    {
        if (!_barCache.TryGetValue(bar.SymbolId, out var tfCache))
        {
            tfCache = new Dictionary<Timeframe, List<Bar>>();
            _barCache[bar.SymbolId] = tfCache;
        }

        if (!tfCache.TryGetValue(bar.Timeframe, out var bars))
        {
            bars = new List<Bar>();
            tfCache[bar.Timeframe] = bars;
        }

        bars.Add(bar);

        // Keep max 1000 bars per timeframe
        if (bars.Count > 1000)
            bars.RemoveAt(0);
    }

    private SignalContext? BuildContext(SymbolId symbolId, ISignalStrategy strategy)
    {
        if (!_barCache.TryGetValue(symbolId, out var tfCache))
            return null;

        var marketData = new Dictionary<Timeframe, IReadOnlyList<Bar>>();

        foreach (var (tf, count) in strategy.RequiredTimeframes)
        {
            if (!tfCache.TryGetValue(tf, out var bars) || bars.Count < count)
                return null;

            marketData[tf] = bars.TakeLast(count).ToList();
        }

        return new SignalContext
        {
            SymbolId = symbolId,
            Symbol = symbolId.ToString(), // TODO: Lookup from instrument
            MarketData = marketData,
            AccountBalance = 10000m, // TODO: Get from portfolio
            Timestamp = _clock.UtcNow
        };
    }

    private async Task ProcessSignalAsync(
        SymbolId symbolId,
        ISignalStrategy strategy,
        SignalResult signal,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Signal: {Signal} for {Symbol} from {Strategy} - {Reason}",
            signal.Signal, symbolId, strategy.Name, signal.Reason);

        // Convert signal to order side
        var side = signal.Signal switch
        {
            TradeSignal.Buy => OrderSide.Buy,
            TradeSignal.Sell => OrderSide.Sell,
            _ => throw new InvalidOperationException($"Invalid signal for order: {signal.Signal}")
        };

        // Use suggested values or defaults
        var volume = signal.SuggestedVolume ?? 0.01;
        var stopLoss = signal.SuggestedStopLossPips;
        var takeProfit = signal.SuggestedTakeProfitPips;

        // Generate client order ID for traceability
        var clientOrderId = $"{strategy.Name}-{_clock.UnixTimeSeconds}";

        try
        {
            // Place order
            var order = await _orderExecution.PlaceOrderAsync(
                symbolId,
                side,
                OrderType.Market,
                volume,
                stopLoss: stopLoss,
                takeProfit: takeProfit,
                clientOrderId: clientOrderId,
                ct: ct);

            _logger.LogInformation(
                "Order placed: {OrderId} {Side} {Volume} lots for {Symbol}",
                order.OrderId, order.Side, order.Volume, symbolId);

            // Publish events
            await _eventSink.PublishOrderAsync(order, ct);
            await _eventSink.PublishEngineStatusAsync(
                $"Order placed: {side} {volume} lots @ {symbolId}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place order for {Symbol} from {Strategy}",
                symbolId, strategy.Name);
            await _eventSink.PublishEngineStatusAsync(
                $"Order failed: {ex.Message}", ct);
        }
    }

    /// <summary>
    /// Gets the number of registered strategies.
    /// </summary>
    public int StrategyCount => _strategies.Count;

    /// <summary>
    /// Gets the cached bar count for a symbol and timeframe.
    /// </summary>
    public int GetCachedBarCount(SymbolId symbolId, Timeframe timeframe)
    {
        if (_barCache.TryGetValue(symbolId, out var tfCache) &&
            tfCache.TryGetValue(timeframe, out var bars))
        {
            return bars.Count;
        }
        return 0;
    }
}
