using Alphiq.Configuration.Abstractions;
using Alphiq.TradingEngine.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Alphiq.TradingEngine.Factories;

/// <summary>
/// Factory for creating strategy instances from definitions.
/// </summary>
public interface IStrategyFactory
{
    /// <summary>
    /// Attempts to create a strategy from a definition.
    /// </summary>
    bool TryCreate(StrategyDefinition definition, out ISignalStrategy? strategy);

    /// <summary>
    /// Registers a strategy type with a name.
    /// </summary>
    void Register<TStrategy>(string name) where TStrategy : ISignalStrategy;

    /// <summary>
    /// Registers a strategy factory function with a name.
    /// </summary>
    void Register(string name, Func<StrategyDefinition, ISignalStrategy> factory);
}

/// <summary>
/// Simple dictionary-based strategy factory.
/// </summary>
public sealed class StrategyFactory : IStrategyFactory
{
    private readonly Dictionary<string, Func<StrategyDefinition, ISignalStrategy>> _registry = new(StringComparer.OrdinalIgnoreCase);
    private readonly IServiceProvider _services;
    private readonly ILogger<StrategyFactory> _logger;

    public StrategyFactory(IServiceProvider services, ILogger<StrategyFactory> logger)
    {
        _services = services;
        _logger = logger;
    }

    public void Register<TStrategy>(string name) where TStrategy : ISignalStrategy
    {
        _registry[name] = def => ActivatorUtilities.CreateInstance<TStrategy>(_services, def);
        _logger.LogDebug("Registered strategy type {Type} with name '{Name}'", typeof(TStrategy).Name, name);
    }

    public void Register(string name, Func<StrategyDefinition, ISignalStrategy> factory)
    {
        _registry[name] = factory;
        _logger.LogDebug("Registered strategy factory with name '{Name}'", name);
    }

    public bool TryCreate(StrategyDefinition definition, out ISignalStrategy? strategy)
    {
        strategy = null;

        if (!_registry.TryGetValue(definition.Name, out var factory))
        {
            _logger.LogWarning("No strategy registered with name '{Name}'. Available: {Available}",
                definition.Name, string.Join(", ", _registry.Keys));
            return false;
        }

        try
        {
            strategy = factory(definition);
            _logger.LogInformation("Created strategy '{Name}' v{Version}", definition.Name, definition.Version);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create strategy '{Name}'", definition.Name);
            return false;
        }
    }
}
