using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Risk;

/// <summary>
/// Fixed pips stop loss strategy.
/// Always returns the same stop loss distance regardless of market conditions.
/// </summary>
public sealed class FixedPipsStopLoss : IStopLossStrategy
{
    private readonly double _pips;

    /// <summary>
    /// Creates a fixed pips stop loss strategy.
    /// </summary>
    /// <param name="pips">The stop loss distance in pips.</param>
    /// <exception cref="ArgumentOutOfRangeException">When pips is less than or equal to zero.</exception>
    public FixedPipsStopLoss(double pips)
    {
        if (pips <= 0)
            throw new ArgumentOutOfRangeException(nameof(pips), "Pips must be greater than zero.");

        _pips = pips;
    }

    /// <summary>
    /// Gets the configured stop loss pips.
    /// </summary>
    public double Pips => _pips;

    /// <inheritdoc />
    public double CalculateStopLossPips(SignalContext context) => _pips;
}
