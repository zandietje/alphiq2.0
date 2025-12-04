using Alphiq.Domain.Entities;
using Alphiq.Domain.ValueObjects;
using Alphiq.Infrastructure.Supabase.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Alphiq.Infrastructure.Supabase.Repositories;

/// <summary>
/// Repository for fetching candle/bar data from Supabase using Npgsql.
/// Uses direct SQL for efficient bulk data retrieval required by backtesting.
/// </summary>
public sealed class CandleRepository : ICandleRepository
{
    private readonly string _connectionString;
    private readonly ILogger<CandleRepository> _logger;

    public CandleRepository(IOptions<SupabaseOptions> options, ILogger<CandleRepository> logger)
    {
        var connectionString = options.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Supabase ConnectionString is required for CandleRepository. " +
                "Configure it in the Supabase configuration section.");
        }

        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Bar>> GetBarsAsync(
        SymbolId symbolId,
        Timeframe timeframe,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        var fromUnix = from.ToUnixTimeSeconds();
        var toUnix = to.ToUnixTimeSeconds();

        _logger.LogDebug(
            "Fetching bars for symbol {SymbolId}, timeframe {Timeframe}, from {From} to {To}",
            symbolId.Value, timeframe.Code, from, to);

        var bars = new List<Bar>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        const string sql = """
            SELECT timestamp, symbol_id, timeframe, open, high, low, close, volume
            FROM candles
            WHERE symbol_id = @symbolId
              AND timeframe = @timeframe
              AND timestamp >= @from
              AND timestamp <= @to
            ORDER BY timestamp ASC
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@symbolId", symbolId.Value);
        cmd.Parameters.AddWithValue("@timeframe", timeframe.Code);
        cmd.Parameters.AddWithValue("@from", fromUnix);
        cmd.Parameters.AddWithValue("@to", toUnix);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var bar = new Bar
            {
                Timestamp = reader.GetInt64(0),
                SymbolId = new SymbolId(reader.GetInt64(1)),
                Timeframe = Timeframe.Parse(reader.GetString(2)),
                Open = reader.GetDouble(3),
                High = reader.GetDouble(4),
                Low = reader.GetDouble(5),
                Close = reader.GetDouble(6),
                Volume = reader.GetDouble(7)
            };
            bars.Add(bar);
        }

        _logger.LogInformation(
            "Fetched {Count} bars for symbol {SymbolId}, timeframe {Timeframe}",
            bars.Count, symbolId.Value, timeframe.Code);

        return bars;
    }
}
