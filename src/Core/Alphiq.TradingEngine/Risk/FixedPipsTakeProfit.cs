using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Risk;

/// <summary>
/// Fixed pips take profit strategy.
/// Always returns the same take profit distance regardless of market conditions or stop loss.
/// </summary>
public sealed class FixedPipsTakeProfit : ITakeProfitStrategy
{
    private readonly double _pips;

    /// <summary>
    /// Creates a fixed pips take profit strategy.
    /// </summary>
    /// <param name="pips">The take profit distance in pips.</param>
    /// <exception cref="ArgumentOutOfRangeException">When pips is less than or equal to zero.</exception>
    public FixedPipsTakeProfit(double pips)
    {
        if (pips <= 0)
            throw new ArgumentOutOfRangeException(nameof(pips), "Pips must be greater than zero.");

        _pips = pips;
    }

    /// <summary>
    /// Gets the configured take profit pips.
    /// </summary>
    public double Pips => _pips;

    /// <inheritdoc />
    public double CalculateTakeProfitPips(SignalContext context, double stopLossPips) => _pips;
}
