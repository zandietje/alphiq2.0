using Alphiq.Configuration.Abstractions;
using Alphiq.Infrastructure.Supabase.Repositories;

namespace Alphiq.Infrastructure.Supabase;

/// <summary>
/// Supabase-backed implementation of <see cref="IStrategyConfigProvider"/>.
/// </summary>
public sealed class SupabaseStrategyConfigProvider : IStrategyConfigProvider
{
    private readonly IStrategyRepository _repository;

    public SupabaseStrategyConfigProvider(IStrategyRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<StrategyDefinition>> LoadAllAsync(CancellationToken ct = default)
    {
        return _repository.GetAllEnabledAsync(ct);
    }

    /// <inheritdoc />
    public Task<StrategyDefinition?> LoadByNameAsync(string name, CancellationToken ct = default)
    {
        return _repository.GetByNameAsync(name, ct);
    }
}
