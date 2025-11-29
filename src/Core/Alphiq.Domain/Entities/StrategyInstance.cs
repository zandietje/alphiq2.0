using Alphiq.Domain.ValueObjects;

namespace Alphiq.Domain.Entities;

/// <summary>
/// Running strategy instance state.
/// </summary>
public sealed record StrategyInstance
{
    public required string InstanceId { get; init; }
    public required string StrategyName { get; init; }
    public required int Version { get; init; }
    public required IReadOnlyList<SymbolId> Symbols { get; init; }
    public required Timeframe MainTimeframe { get; init; }
    public required bool IsActive { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
}
