using Alphiq.Brokers.Abstractions;

namespace Alphiq.TradingEngine.Adapters;

/// <summary>
/// Controllable clock for testing.
/// Allows setting the current time for deterministic tests.
/// </summary>
public sealed class TestClock : IClock
{
    private DateTimeOffset _utcNow;

    /// <summary>
    /// Creates a test clock set to the current time.
    /// </summary>
    public TestClock() : this(DateTimeOffset.UtcNow)
    {
    }

    /// <summary>
    /// Creates a test clock set to a specific time.
    /// </summary>
    public TestClock(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    /// <summary>
    /// Gets or sets the current UTC time.
    /// </summary>
    public DateTimeOffset UtcNow
    {
        get => _utcNow;
        set => _utcNow = value;
    }

    /// <summary>
    /// Advances the clock by the specified duration.
    /// </summary>
    public void Advance(TimeSpan duration)
    {
        _utcNow = _utcNow.Add(duration);
    }

    /// <summary>
    /// Sets the clock to a specific time.
    /// </summary>
    public void SetTime(DateTimeOffset time)
    {
        _utcNow = time;
    }

    /// <summary>
    /// Sets the clock to a specific Unix timestamp in seconds.
    /// </summary>
    public void SetTimeFromUnixSeconds(long unixSeconds)
    {
        _utcNow = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
    }
}
