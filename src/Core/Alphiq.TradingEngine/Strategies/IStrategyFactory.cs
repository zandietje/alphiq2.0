using Alphiq.Configuration.Abstractions;

namespace Alphiq.TradingEngine.Strategies;

/// <summary>
/// Factory for creating strategy instances from strategy definitions.
/// </summary>
public interface IStrategyFactory
{
    /// <summary>
    /// Creates a strategy instance from a strategy definition.
    /// </summary>
    /// <param name="definition">The strategy definition containing configuration.</param>
    /// <returns>The created strategy instance, or null if the strategy type is not supported.</returns>
    ISignalStrategy? Create(StrategyDefinition definition);

    /// <summary>
    /// Creates a strategy instance by name using default configuration.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to create.</param>
    /// <returns>The created strategy instance, or null if the strategy is not found.</returns>
    ISignalStrategy? CreateByName(string strategyName);
}
