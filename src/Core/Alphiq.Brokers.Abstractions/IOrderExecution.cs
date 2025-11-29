using Alphiq.Domain.Entities;
using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.Brokers.Abstractions;

/// <summary>
/// Order execution abstraction for live/simulated trading.
/// </summary>
public interface IOrderExecution
{
    /// <summary>
    /// Places a new order.
    /// </summary>
    Task<Order> PlaceOrderAsync(
        SymbolId symbolId,
        OrderSide side,
        OrderType type,
        Quantity volume,
        double? price = null,
        double? stopLoss = null,
        double? takeProfit = null,
        string? clientOrderId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Modifies an existing order.
    /// </summary>
    Task<Order> ModifyOrderAsync(
        string orderId,
        double? stopLoss = null,
        double? takeProfit = null,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels an order.
    /// </summary>
    Task CancelOrderAsync(string orderId, CancellationToken ct = default);

    /// <summary>
    /// Gets current open positions.
    /// </summary>
    Task<IReadOnlyList<Position>> GetPositionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Closes a position.
    /// </summary>
    Task ClosePositionAsync(string positionId, CancellationToken ct = default);
}
