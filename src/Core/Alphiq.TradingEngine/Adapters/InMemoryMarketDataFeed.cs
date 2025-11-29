using System.Runtime.CompilerServices;
using Alphiq.Brokers.Abstractions;
using Alphiq.Domain.Entities;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.TradingEngine.Adapters;

/// <summary>
/// In-memory market data feed for testing.
/// Allows pushing bars programmatically.
/// </summary>
public sealed class InMemoryMarketDataFeed : IMarketDataFeed
{
    private readonly Dictionary<(SymbolId, Timeframe), List<Bar>> _history = new();
    private readonly Dictionary<(SymbolId, Timeframe), List<Bar>> _streamedBars = new();

    /// <summary>
    /// Adds historical bars that will be returned by GetHistoryAsync.
    /// </summary>
    public void AddHistory(SymbolId symbolId, Timeframe timeframe, IEnumerable<Bar> bars)
    {
        var key = (symbolId, timeframe);
        if (!_history.TryGetValue(key, out var list))
        {
            list = new List<Bar>();
            _history[key] = list;
        }
        list.AddRange(bars);
    }

    /// <summary>
    /// Adds bars to be streamed via SubscribeBarsAsync.
    /// </summary>
    public void AddStreamBars(SymbolId symbolId, Timeframe timeframe, IEnumerable<Bar> bars)
    {
        var key = (symbolId, timeframe);
        if (!_streamedBars.TryGetValue(key, out var list))
        {
            list = new List<Bar>();
            _streamedBars[key] = list;
        }
        list.AddRange(bars);
    }

    public async IAsyncEnumerable<Bar> SubscribeBarsAsync(
        SymbolId symbolId,
        Timeframe timeframe,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var key = (symbolId, timeframe);
        if (_streamedBars.TryGetValue(key, out var bars))
        {
            foreach (var bar in bars)
            {
                if (ct.IsCancellationRequested)
                    yield break;
                yield return bar;
                await Task.Yield(); // Allow cancellation checks
            }
        }
    }

    public async IAsyncEnumerable<Tick> SubscribeTicksAsync(
        SymbolId symbolId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Not needed for basic engine testing
        await Task.CompletedTask;
        yield break;
    }

    public Task<IReadOnlyList<Bar>> GetHistoryAsync(
        SymbolId symbolId,
        Timeframe timeframe,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        var key = (symbolId, timeframe);
        if (_history.TryGetValue(key, out var bars))
        {
            var filtered = bars
                .Where(b => b.DateTime >= from && b.DateTime <= to)
                .OrderBy(b => b.Timestamp)
                .ToList();
            return Task.FromResult<IReadOnlyList<Bar>>(filtered);
        }

        return Task.FromResult<IReadOnlyList<Bar>>(Array.Empty<Bar>());
    }

    /// <summary>
    /// Clears all stored data. Useful for test cleanup.
    /// </summary>
    public void Clear()
    {
        _history.Clear();
        _streamedBars.Clear();
    }
}
