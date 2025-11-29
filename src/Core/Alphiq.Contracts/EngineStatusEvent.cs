namespace Alphiq.Contracts;

/// <summary>
/// NATS message for engine status updates.
/// </summary>
public sealed record EngineStatusEvent
{
    public required string EngineId { get; init; }
    public required string Status { get; init; }
    public required int ActiveStrategies { get; init; }
    public required int OpenPositions { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
