using Alphiq.Brokers.Abstractions;
using Alphiq.Domain.Entities;
using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.TradingEngine.Adapters;

/// <summary>
/// In-memory order execution adapter for testing.
/// Records all placed orders for verification.
/// </summary>
public sealed class InMemoryOrderExecution : IOrderExecution
{
    private readonly List<Order> _placedOrders = new();
    private readonly List<Position> _positions = new();
    private readonly IClock _clock;

    public InMemoryOrderExecution(IClock? clock = null)
    {
        _clock = clock ?? new SystemClock();
    }

    /// <summary>
    /// All orders that have been placed.
    /// </summary>
    public IReadOnlyList<Order> PlacedOrders => _placedOrders;

    /// <summary>
    /// Current open positions.
    /// </summary>
    public IReadOnlyList<Position> Positions => _positions;

    public Task<Order> PlaceOrderAsync(
        SymbolId symbolId,
        OrderSide side,
        OrderType type,
        Quantity volume,
        double? price = null,
        double? stopLoss = null,
        double? takeProfit = null,
        string? clientOrderId = null,
        CancellationToken ct = default)
    {
        var order = new Order
        {
            OrderId = Guid.NewGuid().ToString("N")[..8],
            SymbolId = symbolId,
            Side = side,
            Type = type,
            Volume = volume,
            Price = price,
            StopLoss = stopLoss,
            TakeProfit = takeProfit,
            Status = OrderStatus.Filled, // Simulated immediate fill
            CreatedAt = _clock.UtcNow,
            ClientOrderId = clientOrderId
        };

        _placedOrders.Add(order);

        // Also create a position for tracking
        var position = new Position
        {
            PositionId = Guid.NewGuid().ToString("N")[..8],
            SymbolId = symbolId,
            Side = side,
            Volume = volume,
            EntryPrice = price ?? 1.0,
            StopLoss = stopLoss,
            TakeProfit = takeProfit,
            OpenedAt = _clock.UtcNow,
            StrategyName = clientOrderId?.Split('-').FirstOrDefault()
        };
        _positions.Add(position);

        return Task.FromResult(order);
    }

    public Task<Order> ModifyOrderAsync(
        string orderId,
        double? stopLoss = null,
        double? takeProfit = null,
        CancellationToken ct = default)
    {
        var order = _placedOrders.FirstOrDefault(o => o.OrderId == orderId);
        if (order is null)
            throw new InvalidOperationException($"Order {orderId} not found");

        var modified = order with
        {
            StopLoss = stopLoss ?? order.StopLoss,
            TakeProfit = takeProfit ?? order.TakeProfit
        };

        var index = _placedOrders.IndexOf(order);
        _placedOrders[index] = modified;

        return Task.FromResult(modified);
    }

    public Task CancelOrderAsync(string orderId, CancellationToken ct = default)
    {
        var order = _placedOrders.FirstOrDefault(o => o.OrderId == orderId);
        if (order is not null)
        {
            var index = _placedOrders.IndexOf(order);
            _placedOrders[index] = order with { Status = OrderStatus.Cancelled };
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Position>> GetPositionsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<Position>>(_positions);
    }

    public Task ClosePositionAsync(string positionId, CancellationToken ct = default)
    {
        var position = _positions.FirstOrDefault(p => p.PositionId == positionId);
        if (position is not null)
        {
            _positions.Remove(position);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all recorded orders and positions. Useful for test cleanup.
    /// </summary>
    public void Clear()
    {
        _placedOrders.Clear();
        _positions.Clear();
    }
}
