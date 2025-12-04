namespace Alphiq.Infrastructure.Messaging;

/// <summary>
/// Wrapper for received NATS JetStream messages with acknowledgment support.
/// </summary>
/// <typeparam name="T">The message data type.</typeparam>
public sealed class NatsMessage<T> where T : class
{
    /// <summary>
    /// The deserialized message data.
    /// </summary>
    public required T Data { get; init; }

    /// <summary>
    /// Acknowledges successful processing of the message.
    /// Must be called after processing to prevent redelivery.
    /// </summary>
    public required Func<CancellationToken, ValueTask> AckAsync { get; init; }

    /// <summary>
    /// Negative acknowledges the message, requesting redelivery.
    /// Use when processing fails and the message should be retried.
    /// </summary>
    public required Func<CancellationToken, ValueTask> NakAsync { get; init; }
}
