using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.Infrastructure.Broker.Simulated.Models;

/// <summary>
/// Model representing an open position in the simulation.
/// Tracks entry bar timestamp for T+1 SL/TP rule enforcement.
/// </summary>
public sealed record SimulatedPosition
{
    public required string PositionId { get; init; }
    public required SymbolId SymbolId { get; init; }
    public required OrderSide Side { get; init; }
    public required Quantity Volume { get; init; }
    public required double EntryPrice { get; init; }
    public double? StopLoss { get; init; }
    public double? TakeProfit { get; init; }

    /// <summary>
    /// Unix timestamp of the bar when this position was filled.
    /// Used to enforce T+1 rule: SL/TP cannot trigger on entry bar.
    /// </summary>
    public required long EntryBarTimestamp { get; init; }

    public required DateTimeOffset OpenedAt { get; init; }
    public string? StrategyName { get; init; }
}
