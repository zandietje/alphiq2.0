using Alphiq.Configuration.Abstractions;
using Microsoft.Extensions.Logging;

namespace Alphiq.TradingEngine.Strategies;

/// <summary>
/// Default strategy factory implementation.
/// Creates strategy instances based on strategy names.
/// </summary>
public sealed class StrategyFactory : IStrategyFactory
{
    private readonly ILogger<StrategyFactory> _logger;

    public StrategyFactory(ILogger<StrategyFactory> logger)
    {
        _logger = logger;
    }

    public ISignalStrategy? Create(StrategyDefinition definition)
    {
        _logger.LogDebug("Creating strategy {Name} v{Version}", definition.Name, definition.Version);

        // Match by name (case-insensitive)
        var strategy = definition.Name.ToUpperInvariant() switch
        {
            "BUYONFIRSTBAR" => new BuyOnFirstBarStrategy(definition),
            // Add more strategies here as they are implemented
            _ => null
        };

        if (strategy is null)
        {
            _logger.LogWarning("Unknown strategy type: {Name}", definition.Name);
        }

        return strategy;
    }

    public ISignalStrategy? CreateByName(string strategyName)
    {
        _logger.LogDebug("Creating strategy by name: {Name}", strategyName);

        // Match by name (case-insensitive) with default configuration
        var strategy = strategyName.ToUpperInvariant() switch
        {
            "BUYONFIRSTBAR" => new BuyOnFirstBarStrategy(),
            // Add more strategies here as they are implemented
            _ => null
        };

        if (strategy is null)
        {
            _logger.LogWarning("Unknown strategy name: {Name}", strategyName);
        }

        return strategy;
    }
}
