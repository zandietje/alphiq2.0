using Alphiq.Domain.Entities;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.Brokers.Abstractions;

/// <summary>
/// Market data feed abstraction for live/historical/simulated data.
/// </summary>
public interface IMarketDataFeed
{
    /// <summary>
    /// Subscribes to real-time bar updates.
    /// </summary>
    IAsyncEnumerable<Bar> SubscribeBarsAsync(
        SymbolId symbolId,
        Timeframe timeframe,
        CancellationToken ct = default);

    /// <summary>
    /// Subscribes to real-time tick updates.
    /// </summary>
    IAsyncEnumerable<Tick> SubscribeTicksAsync(
        SymbolId symbolId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets historical bars.
    /// </summary>
    Task<IReadOnlyList<Bar>> GetHistoryAsync(
        SymbolId symbolId,
        Timeframe timeframe,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);
}
