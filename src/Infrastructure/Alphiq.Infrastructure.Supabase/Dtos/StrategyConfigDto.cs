using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alphiq.Infrastructure.Supabase.Dtos;

/// <summary>
/// DTO for strategy_configs table rows from Supabase PostgREST API.
/// </summary>
public sealed record StrategyConfigDto
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("version")]
    public int Version { get; init; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("main_timeframe")]
    public required string MainTimeframe { get; init; }

    [JsonPropertyName("config")]
    public JsonElement Config { get; init; }

    [JsonPropertyName("symbol_list")]
    public required string[] SymbolList { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}
