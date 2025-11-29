namespace Alphiq.Configuration.Abstractions;

/// <summary>
/// Strategy configuration provider.
/// </summary>
public interface IStrategyConfigProvider
{
    /// <summary>
    /// Loads all enabled strategy configurations.
    /// </summary>
    Task<IReadOnlyList<StrategyDefinition>> LoadAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads a specific strategy by name.
    /// </summary>
    Task<StrategyDefinition?> LoadByNameAsync(string name, CancellationToken ct = default);
}
