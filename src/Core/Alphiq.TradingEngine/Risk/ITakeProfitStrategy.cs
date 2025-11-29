using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Risk;

/// <summary>
/// Take profit calculation strategy.
/// </summary>
public interface ITakeProfitStrategy
{
    /// <summary>
    /// Calculates take profit distance in pips.
    /// </summary>
    double CalculateTakeProfitPips(SignalContext context, double stopLossPips);
}
