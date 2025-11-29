using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.Contracts;

/// <summary>
/// NATS message for trade events.
/// </summary>
public sealed record TradeEvent
{
    public required string TradeId { get; init; }
    public required SymbolId SymbolId { get; init; }
    public required string Symbol { get; init; }
    public required OrderSide Side { get; init; }
    public required double Volume { get; init; }
    public required double Price { get; init; }
    public required string StrategyName { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
