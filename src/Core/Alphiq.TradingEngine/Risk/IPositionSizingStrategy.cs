using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Risk;

/// <summary>
/// Position sizing calculation strategy.
/// </summary>
public interface IPositionSizingStrategy
{
    /// <summary>
    /// Calculates position size in lots.
    /// </summary>
    double CalculateVolume(SignalContext context, double stopLossPips);
}
