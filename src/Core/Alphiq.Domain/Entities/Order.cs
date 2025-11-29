using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.Domain.Entities;

/// <summary>
/// Trading order.
/// </summary>
public sealed record Order
{
    public required string OrderId { get; init; }
    public required SymbolId SymbolId { get; init; }
    public required OrderSide Side { get; init; }
    public required OrderType Type { get; init; }
    public required Quantity Volume { get; init; }
    public double? Price { get; init; }
    public double? StopLoss { get; init; }
    public double? TakeProfit { get; init; }
    public required OrderStatus Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? StrategyName { get; init; }
    public string? ClientOrderId { get; init; }
}
