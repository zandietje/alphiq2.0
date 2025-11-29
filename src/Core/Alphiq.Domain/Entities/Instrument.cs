using Alphiq.Domain.ValueObjects;

namespace Alphiq.Domain.Entities;

/// <summary>
/// Trading instrument metadata.
/// </summary>
public sealed record Instrument
{
    public required SymbolId Id { get; init; }
    public required string Symbol { get; init; }
    public required string Description { get; init; }
    public required int Digits { get; init; }
    public required double PipPosition { get; init; }
    public required double MinVolume { get; init; }
    public required double MaxVolume { get; init; }
    public required double VolumeStep { get; init; }
}
