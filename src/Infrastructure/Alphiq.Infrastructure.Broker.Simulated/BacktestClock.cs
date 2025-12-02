using Alphiq.Brokers.Abstractions;
using Alphiq.Domain.Entities;

namespace Alphiq.Infrastructure.Broker.Simulated;

/// <summary>
/// Advanceable clock for backtesting simulations.
/// Time is advanced by the backtest orchestrator and never goes backwards.
/// </summary>
public sealed class BacktestClock : IClock
{
    private DateTimeOffset _currentTime;

    /// <summary>
    /// Creates a backtest clock initialized to MinValue (must be advanced before use).
    /// </summary>
    public BacktestClock() : this(DateTimeOffset.MinValue)
    {
    }

    /// <summary>
    /// Creates a backtest clock initialized to a specific start time.
    /// </summary>
    public BacktestClock(DateTimeOffset startTime)
    {
        _currentTime = startTime;
    }

    /// <inheritdoc />
    public DateTimeOffset UtcNow => _currentTime;

    /// <summary>
    /// Advances the clock to the specified time.
    /// Throws if attempting to move backwards in time.
    /// </summary>
    /// <param name="time">The new time (must be >= current time).</param>
    /// <exception cref="InvalidOperationException">Thrown when attempting to move backwards.</exception>
    public void AdvanceTo(DateTimeOffset time)
    {
        if (time < _currentTime)
        {
            throw new InvalidOperationException(
                $"Cannot move clock backwards from {_currentTime:O} to {time:O}");
        }
        _currentTime = time;
    }

    /// <summary>
    /// Advances the clock to the bar's close time (DateTime property).
    /// </summary>
    public void AdvanceToBarClose(Bar bar)
    {
        AdvanceTo(bar.DateTime);
    }

    /// <summary>
    /// Sets the clock to a specific time without validation.
    /// Use only for test setup, not during simulation.
    /// </summary>
    public void Reset(DateTimeOffset time)
    {
        _currentTime = time;
    }
}
