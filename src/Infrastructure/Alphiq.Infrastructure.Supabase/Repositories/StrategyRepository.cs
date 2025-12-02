using System.Net.Http.Json;
using System.Text.Json;
using Alphiq.Configuration.Abstractions;
using Alphiq.Domain.ValueObjects;
using Alphiq.Infrastructure.Supabase.Dtos;
using Microsoft.Extensions.Logging;

namespace Alphiq.Infrastructure.Supabase.Repositories;

/// <summary>
/// Repository for loading strategy configurations from Supabase via PostgREST API.
/// </summary>
public sealed class StrategyRepository : IStrategyRepository
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StrategyRepository> _logger;

    public StrategyRepository(HttpClient httpClient, ILogger<StrategyRepository> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<StrategyDefinition>> GetAllEnabledAsync(CancellationToken ct = default)
    {
        const string requestUri = "rest/v1/strategy_configs?enabled=eq.true&order=name.asc,version.desc";

        _logger.LogDebug("Fetching enabled strategy configs from Supabase");

        var response = await _httpClient.GetAsync(requestUri, ct);
        response.EnsureSuccessStatusCode();

        var dtos = await response.Content.ReadFromJsonAsync<List<StrategyConfigDto>>(ct);

        if (dtos is null || dtos.Count == 0)
        {
            _logger.LogWarning("No enabled strategy configs found");
            return [];
        }

        // Group by name, take first (already sorted by version desc) - gets latest version
        var latestPerStrategy = dtos
            .GroupBy(d => d.Name)
            .Select(g => g.First())
            .ToList();

        _logger.LogInformation("Loaded {Count} strategy configs: {Names}",
            latestPerStrategy.Count,
            string.Join(", ", latestPerStrategy.Select(s => $"{s.Name} v{s.Version}")));

        return latestPerStrategy.Select(MapToDomain).ToList();
    }

    public async Task<StrategyDefinition?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var requestUri = $"rest/v1/strategy_configs?name=eq.{Uri.EscapeDataString(name)}&enabled=eq.true&order=version.desc&limit=1";

        _logger.LogDebug("Fetching strategy config '{Name}' from Supabase", name);

        var response = await _httpClient.GetAsync(requestUri, ct);
        response.EnsureSuccessStatusCode();

        var dtos = await response.Content.ReadFromJsonAsync<List<StrategyConfigDto>>(ct);

        if (dtos is null || dtos.Count == 0)
        {
            _logger.LogDebug("Strategy config '{Name}' not found", name);
            return null;
        }

        var dto = dtos[0];
        _logger.LogInformation("Loaded strategy config '{Name}' v{Version}", dto.Name, dto.Version);

        return MapToDomain(dto);
    }

    private static StrategyDefinition MapToDomain(StrategyConfigDto dto)
    {
        // Parse main timeframe
        var mainTimeframe = Timeframe.Parse(dto.MainTimeframe);

        // Parse symbol list: TEXT[] -> IReadOnlyList<SymbolId>
        var symbols = dto.SymbolList
            .Select(s => new SymbolId(long.Parse(s)))
            .ToList();

        // Extract nested properties from config JSON
        var timeframesDict = ParseTimeframes(dto.Config);
        var parameters = ParseParameters(dto.Config);
        var risk = ParseRiskConfig(dto.Config);

        return new StrategyDefinition
        {
            Name = dto.Name,
            Version = dto.Version,
            MainTimeframe = mainTimeframe,
            RequiredTimeframes = timeframesDict,
            Parameters = parameters,
            Risk = risk,
            Symbols = symbols,
            Enabled = dto.Enabled
        };
    }

    private static IReadOnlyDictionary<Timeframe, int> ParseTimeframes(JsonElement config)
    {
        if (!config.TryGetProperty("Timeframes", out var tfElement))
            return new Dictionary<Timeframe, int>();

        var result = new Dictionary<Timeframe, int>();
        foreach (var prop in tfElement.EnumerateObject())
        {
            var tf = Timeframe.Parse(prop.Name);
            var count = prop.Value.GetInt32();
            result[tf] = count;
        }
        return result;
    }

    private static IReadOnlyDictionary<string, JsonElement> ParseParameters(JsonElement config)
    {
        if (!config.TryGetProperty("Parameters", out var paramsElement))
            return new Dictionary<string, JsonElement>();

        var result = new Dictionary<string, JsonElement>();
        foreach (var prop in paramsElement.EnumerateObject())
        {
            result[prop.Name] = prop.Value.Clone();
        }
        return result;
    }

    private static RiskConfig ParseRiskConfig(JsonElement config)
    {
        var riskElement = config.GetProperty("Risk");

        return new RiskConfig
        {
            StopLoss = ParseComponentConfig<StopLossConfig>(riskElement.GetProperty("StopLoss")),
            TakeProfit = ParseComponentConfig<TakeProfitConfig>(riskElement.GetProperty("TakeProfit")),
            PositionSizing = ParseComponentConfig<PositionSizingConfig>(riskElement.GetProperty("PositionSizing"))
        };
    }

    private static T ParseComponentConfig<T>(JsonElement element) where T : class
    {
        var type = element.GetProperty("Type").GetString()!;
        var parameters = new Dictionary<string, JsonElement>();

        if (element.TryGetProperty("Parameters", out var paramsElement))
        {
            foreach (var prop in paramsElement.EnumerateObject())
            {
                parameters[prop.Name] = prop.Value.Clone();
            }
        }

        // Create instance based on type T
        if (typeof(T) == typeof(StopLossConfig))
        {
            return (new StopLossConfig { Type = type, Parameters = parameters } as T)!;
        }
        if (typeof(T) == typeof(TakeProfitConfig))
        {
            return (new TakeProfitConfig { Type = type, Parameters = parameters } as T)!;
        }
        if (typeof(T) == typeof(PositionSizingConfig))
        {
            return (new PositionSizingConfig { Type = type, Parameters = parameters } as T)!;
        }

        throw new InvalidOperationException($"Unknown config type: {typeof(T).Name}");
    }
}
