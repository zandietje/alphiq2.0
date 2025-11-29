using Alphiq.Domain.ValueObjects;

namespace Alphiq.Contracts;

/// <summary>
/// NATS message for backtest job request.
/// </summary>
public sealed record BacktestJob
{
    public required string JobId { get; init; }
    public required string StrategyName { get; init; }
    public required int StrategyVersion { get; init; }
    public required IReadOnlyList<SymbolId> Symbols { get; init; }
    public required DateTimeOffset StartDate { get; init; }
    public required DateTimeOffset EndDate { get; init; }
    public required IReadOnlyDictionary<string, object> Parameters { get; init; }
    public required DateTimeOffset RequestedAt { get; init; }
}
