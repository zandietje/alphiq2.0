using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.Domain.Entities;

/// <summary>
/// Open trading position.
/// </summary>
public sealed record Position
{
    public required string PositionId { get; init; }
    public required SymbolId SymbolId { get; init; }
    public required OrderSide Side { get; init; }
    public required Quantity Volume { get; init; }
    public required double EntryPrice { get; init; }
    public double? StopLoss { get; init; }
    public double? TakeProfit { get; init; }
    public required DateTimeOffset OpenedAt { get; init; }
    public string? StrategyName { get; init; }

    public Money UnrealizedPnL(double currentPrice)
    {
        var pips = Side == OrderSide.Buy
            ? currentPrice - EntryPrice
            : EntryPrice - currentPrice;
        return new Money((decimal)(pips * Volume.Value * 10), "USD"); // Simplified
    }
}
