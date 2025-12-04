using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Risk;

/// <summary>
/// Risk/reward ratio based take profit strategy.
/// Calculates take profit as a multiple of the stop loss distance.
/// </summary>
public sealed class RiskRewardTakeProfit : ITakeProfitStrategy
{
    private readonly double _riskRewardRatio;

    /// <summary>
    /// Creates a risk/reward based take profit strategy.
    /// </summary>
    /// <param name="riskRewardRatio">The risk/reward ratio (e.g., 2.0 means TP = 2x SL).</param>
    /// <exception cref="ArgumentOutOfRangeException">When ratio is less than or equal to zero.</exception>
    public RiskRewardTakeProfit(double riskRewardRatio)
    {
        if (riskRewardRatio <= 0)
            throw new ArgumentOutOfRangeException(nameof(riskRewardRatio), "Risk/reward ratio must be greater than zero.");

        _riskRewardRatio = riskRewardRatio;
    }

    /// <summary>
    /// Gets the configured risk/reward ratio.
    /// </summary>
    public double RiskRewardRatio => _riskRewardRatio;

    /// <inheritdoc />
    public double CalculateTakeProfitPips(SignalContext context, double stopLossPips)
    {
        if (stopLossPips <= 0)
            throw new ArgumentOutOfRangeException(nameof(stopLossPips), "Stop loss pips must be greater than zero.");

        return stopLossPips * _riskRewardRatio;
    }
}
