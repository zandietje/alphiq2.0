using Alphiq.Configuration.Abstractions;

namespace Alphiq.TradingEngine.Adapters;

/// <summary>
/// In-memory strategy configuration provider for testing.
/// </summary>
public sealed class InMemoryStrategyConfigProvider : IStrategyConfigProvider
{
    private readonly List<StrategyDefinition> _definitions = new();

    /// <summary>
    /// Creates an empty provider.
    /// </summary>
    public InMemoryStrategyConfigProvider()
    {
    }

    /// <summary>
    /// Creates a provider with initial definitions.
    /// </summary>
    public InMemoryStrategyConfigProvider(IEnumerable<StrategyDefinition> definitions)
    {
        _definitions.AddRange(definitions);
    }

    /// <summary>
    /// Adds a strategy definition.
    /// </summary>
    public void Add(StrategyDefinition definition)
    {
        _definitions.Add(definition);
    }

    /// <summary>
    /// Adds multiple strategy definitions.
    /// </summary>
    public void AddRange(IEnumerable<StrategyDefinition> definitions)
    {
        _definitions.AddRange(definitions);
    }

    public Task<IReadOnlyList<StrategyDefinition>> LoadAllAsync(CancellationToken ct = default)
    {
        var enabled = _definitions.Where(d => d.Enabled).ToList();
        return Task.FromResult<IReadOnlyList<StrategyDefinition>>(enabled);
    }

    public Task<StrategyDefinition?> LoadByNameAsync(string name, CancellationToken ct = default)
    {
        var definition = _definitions
            .Where(d => d.Enabled && d.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(d => d.Version)
            .FirstOrDefault();

        return Task.FromResult(definition);
    }

    /// <summary>
    /// Clears all definitions. Useful for test cleanup.
    /// </summary>
    public void Clear()
    {
        _definitions.Clear();
    }
}
