namespace Alphiq.Infrastructure.Broker.Simulated;

/// <summary>
/// Configuration settings for the simulated broker used in backtesting.
/// </summary>
public sealed record BacktestSettings
{
    /// <summary>
    /// Bid/Ask spread in price points (e.g., 0.0004 = ~0.4 pips for forex).
    /// Long orders fill at Open + Spread, short at Open.
    /// </summary>
    public double SpreadPoints { get; init; } = 0.0004;

    /// <summary>
    /// Adverse slippage in price points applied on SL exits.
    /// </summary>
    public double SlippagePoints { get; init; } = 0.0001;

    /// <summary>
    /// Commission per lot per side (entry and exit).
    /// </summary>
    public decimal CommissionPerLot { get; init; } = 3.0m;

    /// <summary>
    /// Initial account balance for the backtest.
    /// </summary>
    public decimal InitialBalance { get; init; } = 10_000m;
}
