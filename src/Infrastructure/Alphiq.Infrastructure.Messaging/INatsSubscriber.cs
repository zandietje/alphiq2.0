namespace Alphiq.Infrastructure.Messaging;

/// <summary>
/// Generic interface for subscribing to NATS JetStream messages.
/// </summary>
/// <typeparam name="T">The message type to subscribe to.</typeparam>
public interface INatsSubscriber<T> where T : class
{
    /// <summary>
    /// Subscribes and yields messages as they arrive from the JetStream consumer.
    /// Messages must be acknowledged after processing by calling AckAsync.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop subscription.</param>
    /// <returns>An async enumerable of messages.</returns>
    IAsyncEnumerable<NatsMessage<T>> SubscribeAsync(CancellationToken cancellationToken = default);
}
