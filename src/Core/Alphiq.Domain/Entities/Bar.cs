using Alphiq.Domain.ValueObjects;

namespace Alphiq.Domain.Entities;

/// <summary>
/// OHLCV bar/candle data.
/// </summary>
public sealed record Bar
{
    public required long Timestamp { get; init; }
    public required SymbolId SymbolId { get; init; }
    public required Timeframe Timeframe { get; init; }
    public required double Open { get; init; }
    public required double High { get; init; }
    public required double Low { get; init; }
    public required double Close { get; init; }
    public required double Volume { get; init; }

    public DateTimeOffset DateTime => DateTimeOffset.FromUnixTimeSeconds(Timestamp);
}
