using Alphiq.Domain.Entities;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.Infrastructure.Supabase.Repositories;

public interface ICandleRepository
{
    Task<IReadOnlyList<Bar>> GetBarsAsync(
        SymbolId symbolId,
        Timeframe timeframe,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);
}
