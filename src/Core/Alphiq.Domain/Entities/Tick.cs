using Alphiq.Domain.ValueObjects;

namespace Alphiq.Domain.Entities;

/// <summary>
/// Real-time price tick.
/// </summary>
public sealed record Tick
{
    public required long Timestamp { get; init; }
    public required SymbolId SymbolId { get; init; }
    public required double Bid { get; init; }
    public required double Ask { get; init; }

    public double Mid => (Bid + Ask) / 2;
    public double Spread => Ask - Bid;
}
