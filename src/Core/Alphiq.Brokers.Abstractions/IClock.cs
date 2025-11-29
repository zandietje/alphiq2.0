namespace Alphiq.Brokers.Abstractions;

/// <summary>
/// Time abstraction for testability.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets current UTC time.
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Gets current Unix timestamp in seconds.
    /// </summary>
    long UnixTimeSeconds => UtcNow.ToUnixTimeSeconds();
}

/// <summary>
/// System clock implementation.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
