namespace Alphiq.Contracts;

/// <summary>
/// NATS message for backtest result.
/// </summary>
public sealed record BacktestResult
{
    public required string JobId { get; init; }
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public required decimal InitialBalance { get; init; }
    public required decimal FinalBalance { get; init; }
    public required int TotalTrades { get; init; }
    public required int WinningTrades { get; init; }
    public required int LosingTrades { get; init; }
    public required decimal ProfitFactor { get; init; }
    public required decimal MaxDrawdownPercent { get; init; }
    public required decimal WinRate { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
}
