using System.Runtime.CompilerServices;
using Alphiq.Brokers.Abstractions;
using Alphiq.Domain.Entities;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.Infrastructure.Broker.Simulated;

/// <summary>
/// Market data feed adapter for backtesting.
/// Loads historical bars and replays them chronologically, coordinating with BacktestClock.
/// </summary>
public sealed class BacktestMarketDataFeed : IMarketDataFeed
{
    private readonly BacktestClock _clock;
    private readonly Dictionary<(SymbolId, Timeframe), List<Bar>> _bars = new();

    public BacktestMarketDataFeed(BacktestClock clock)
    {
        _clock = clock;
    }

    /// <summary>
    /// Loads bars for replay. Bars are automatically sorted by timestamp.
    /// </summary>
    public void LoadBars(SymbolId symbolId, Timeframe timeframe, IEnumerable<Bar> bars)
    {
        var key = (symbolId, timeframe);
        if (!_bars.TryGetValue(key, out var list))
        {
            list = new List<Bar>();
            _bars[key] = list;
        }
        list.AddRange(bars);
        list.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
    }

    /// <summary>
    /// Gets all loaded bars for a symbol/timeframe (for orchestrator iteration).
    /// </summary>
    public IReadOnlyList<Bar> GetAllBars(SymbolId symbolId, Timeframe timeframe)
    {
        var key = (symbolId, timeframe);
        return _bars.TryGetValue(key, out var list) ? list : Array.Empty<Bar>();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Bar> SubscribeBarsAsync(SymbolId symbolId, Timeframe timeframe, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var key = (symbolId, timeframe);
        if (!_bars.TryGetValue(key, out var bars))
            yield break;

        foreach (var bar in bars.OrderBy(b => b.Timestamp))
        {
            if (ct.IsCancellationRequested)
                yield break;

            // Advance clock to bar close time
            _clock.AdvanceToBarClose(bar);

            yield return bar;
            await Task.Yield(); // Allow cancellation checks
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Tick> SubscribeTicksAsync(SymbolId symbolId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Tick-based backtesting not implemented (bar-based only)
        await Task.CompletedTask;
        yield break;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Bar>> GetHistoryAsync(
        SymbolId symbolId,
        Timeframe timeframe,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        var key = (symbolId, timeframe);
        if (!_bars.TryGetValue(key, out var bars))
            return Task.FromResult<IReadOnlyList<Bar>>(Array.Empty<Bar>());

        var filtered = bars
            .Where(b => b.DateTime >= from && b.DateTime <= to)
            .OrderBy(b => b.Timestamp)
            .ToList();

        return Task.FromResult<IReadOnlyList<Bar>>(filtered);
    }

    /// <summary>
    /// Clears all loaded bars. Useful for test cleanup or reset between backtests.
    /// </summary>
    public void Clear()
    {
        _bars.Clear();
    }
}
