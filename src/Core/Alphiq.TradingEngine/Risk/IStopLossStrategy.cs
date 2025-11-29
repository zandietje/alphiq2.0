using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Risk;

/// <summary>
/// Stop loss calculation strategy.
/// </summary>
public interface IStopLossStrategy
{
    /// <summary>
    /// Calculates stop loss distance in pips.
    /// </summary>
    double CalculateStopLossPips(SignalContext context);
}
