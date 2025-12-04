using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Risk;

/// <summary>
/// Risk percent position sizing strategy.
/// Calculates position size to risk a fixed percentage of account balance per trade.
/// Formula: Volume = (Balance * RiskPercent/100) / (StopLossPips * PipValue)
/// </summary>
public sealed class RiskPercentPositionSizing : IPositionSizingStrategy
{
    private readonly double _riskPercent;
    private readonly double _pipValue;

    /// <summary>
    /// Creates a risk percent position sizing strategy.
    /// </summary>
    /// <param name="riskPercent">The percentage of account to risk per trade (e.g., 1.0 = 1%).</param>
    /// <param name="pipValue">The value of one pip in account currency (default: 10.0 for standard forex lot).</param>
    /// <exception cref="ArgumentOutOfRangeException">When parameters are invalid.</exception>
    public RiskPercentPositionSizing(double riskPercent, double pipValue = 10.0)
    {
        if (riskPercent <= 0 || riskPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(riskPercent), "Risk percent must be between 0 and 100.");

        if (pipValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(pipValue), "Pip value must be greater than zero.");

        _riskPercent = riskPercent;
        _pipValue = pipValue;
    }

    /// <summary>
    /// Gets the configured risk percentage.
    /// </summary>
    public double RiskPercent => _riskPercent;

    /// <summary>
    /// Gets the configured pip value.
    /// </summary>
    public double PipValue => _pipValue;

    /// <inheritdoc />
    public double CalculateVolume(SignalContext context, double stopLossPips)
    {
        if (stopLossPips <= 0)
            throw new ArgumentOutOfRangeException(nameof(stopLossPips), "Stop loss pips must be greater than zero.");

        var riskAmount = (double)context.AccountBalance * (_riskPercent / 100.0);
        var volume = riskAmount / (stopLossPips * _pipValue);

        // Round to 2 decimal places (0.01 lot minimum increment)
        return Math.Round(Math.Max(volume, 0.01), 2);
    }
}
