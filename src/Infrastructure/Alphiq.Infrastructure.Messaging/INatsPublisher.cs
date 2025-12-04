namespace Alphiq.Infrastructure.Messaging;

/// <summary>
/// Generic interface for publishing messages to NATS JetStream.
/// </summary>
/// <typeparam name="T">The message type to publish.</typeparam>
public interface INatsPublisher<in T> where T : class
{
    /// <summary>
    /// Publishes a message to the configured JetStream subject.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the message is acknowledged by the server.</returns>
    Task PublishAsync(T message, CancellationToken cancellationToken = default);
}
