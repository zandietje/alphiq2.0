using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.Infrastructure.Broker.Simulated.Models;

/// <summary>
/// Model representing an order awaiting fill at the next bar (T+1 execution).
/// </summary>
public sealed record PendingOrder
{
    public required string OrderId { get; init; }
    public required SymbolId SymbolId { get; init; }
    public required OrderSide Side { get; init; }
    public required OrderType Type { get; init; }
    public required Quantity Volume { get; init; }
    public double? Price { get; init; }
    public double? StopLoss { get; init; }
    public double? TakeProfit { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? ClientOrderId { get; init; }
}
