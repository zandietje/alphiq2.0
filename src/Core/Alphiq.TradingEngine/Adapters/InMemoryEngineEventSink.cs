using Alphiq.Brokers.Abstractions;
using Alphiq.Domain.Entities;

namespace Alphiq.TradingEngine.Adapters;

/// <summary>
/// In-memory event sink for testing.
/// Records all published events for verification.
/// </summary>
public sealed class InMemoryEngineEventSink : IEngineEventSink
{
    private readonly List<Trade> _trades = new();
    private readonly List<Order> _orders = new();
    private readonly List<Position> _positions = new();
    private readonly List<string> _statusMessages = new();

    /// <summary>
    /// All published trades.
    /// </summary>
    public IReadOnlyList<Trade> PublishedTrades => _trades;

    /// <summary>
    /// All published orders.
    /// </summary>
    public IReadOnlyList<Order> PublishedOrders => _orders;

    /// <summary>
    /// All published positions.
    /// </summary>
    public IReadOnlyList<Position> PublishedPositions => _positions;

    /// <summary>
    /// All published status messages.
    /// </summary>
    public IReadOnlyList<string> StatusMessages => _statusMessages;

    public Task PublishTradeAsync(Trade trade, CancellationToken ct = default)
    {
        _trades.Add(trade);
        return Task.CompletedTask;
    }

    public Task PublishOrderAsync(Order order, CancellationToken ct = default)
    {
        _orders.Add(order);
        return Task.CompletedTask;
    }

    public Task PublishPositionAsync(Position position, CancellationToken ct = default)
    {
        _positions.Add(position);
        return Task.CompletedTask;
    }

    public Task PublishEngineStatusAsync(string status, CancellationToken ct = default)
    {
        _statusMessages.Add(status);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all recorded events. Useful for test cleanup.
    /// </summary>
    public void Clear()
    {
        _trades.Clear();
        _orders.Clear();
        _positions.Clear();
        _statusMessages.Clear();
    }
}
