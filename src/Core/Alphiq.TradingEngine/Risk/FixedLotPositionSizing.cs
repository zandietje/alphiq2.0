using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Risk;

/// <summary>
/// Fixed lot position sizing strategy.
/// Always returns the same lot size regardless of account balance or stop loss.
/// </summary>
public sealed class FixedLotPositionSizing : IPositionSizingStrategy
{
    private readonly double _lots;

    /// <summary>
    /// Creates a fixed lot position sizing strategy.
    /// </summary>
    /// <param name="lots">The fixed lot size (e.g., 0.01).</param>
    /// <exception cref="ArgumentOutOfRangeException">When lots is less than or equal to zero.</exception>
    public FixedLotPositionSizing(double lots)
    {
        if (lots <= 0)
            throw new ArgumentOutOfRangeException(nameof(lots), "Lots must be greater than zero.");

        _lots = lots;
    }

    /// <summary>
    /// Gets the configured lot size.
    /// </summary>
    public double Lots => _lots;

    /// <inheritdoc />
    public double CalculateVolume(SignalContext context, double stopLossPips) => _lots;
}
