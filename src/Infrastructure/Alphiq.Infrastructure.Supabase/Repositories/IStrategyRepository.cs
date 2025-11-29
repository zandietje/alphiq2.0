using Alphiq.Configuration.Abstractions;

namespace Alphiq.Infrastructure.Supabase.Repositories;

public interface IStrategyRepository
{
    Task<IReadOnlyList<StrategyDefinition>> GetAllEnabledAsync(CancellationToken ct = default);
    Task<StrategyDefinition?> GetByNameAsync(string name, CancellationToken ct = default);
}
