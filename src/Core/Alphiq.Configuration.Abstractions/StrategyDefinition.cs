using System.Text.Json;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.Configuration.Abstractions;

/// <summary>
/// Strategy configuration definition.
/// </summary>
public sealed record StrategyDefinition
{
    public required string Name { get; init; }
    public required int Version { get; init; }
    public required Timeframe MainTimeframe { get; init; }
    public required IReadOnlyDictionary<Timeframe, int> RequiredTimeframes { get; init; }
    public required IReadOnlyDictionary<string, JsonElement> Parameters { get; init; }
    public required RiskConfig Risk { get; init; }
    public required IReadOnlyList<SymbolId> Symbols { get; init; }
    public bool Enabled { get; init; } = true;
}

public sealed record RiskConfig
{
    public required StopLossConfig StopLoss { get; init; }
    public required TakeProfitConfig TakeProfit { get; init; }
    public required PositionSizingConfig PositionSizing { get; init; }
}

public sealed record StopLossConfig
{
    public required string Type { get; init; }
    public required IReadOnlyDictionary<string, JsonElement> Parameters { get; init; }
}

public sealed record TakeProfitConfig
{
    public required string Type { get; init; }
    public required IReadOnlyDictionary<string, JsonElement> Parameters { get; init; }
}

public sealed record PositionSizingConfig
{
    public required string Type { get; init; }
    public required IReadOnlyDictionary<string, JsonElement> Parameters { get; init; }
}
