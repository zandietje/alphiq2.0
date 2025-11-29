using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.Domain.Entities;

/// <summary>
/// Executed trade/fill.
/// </summary>
public sealed record Trade
{
    public required string TradeId { get; init; }
    public required string OrderId { get; init; }
    public required SymbolId SymbolId { get; init; }
    public required OrderSide Side { get; init; }
    public required Quantity Volume { get; init; }
    public required double Price { get; init; }
    public required Money Commission { get; init; }
    public required DateTimeOffset ExecutedAt { get; init; }
}
