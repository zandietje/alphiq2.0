namespace Alphiq.Infrastructure.Messaging.Testing;

/// <summary>
/// In-memory implementation of INatsPublisher for unit testing.
/// Captures all published messages for verification.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public sealed class InMemoryNatsPublisher<T> : INatsPublisher<T> where T : class
{
    private readonly List<T> _publishedMessages = [];

    /// <summary>
    /// Gets the list of all messages that have been published.
    /// </summary>
    public IReadOnlyList<T> PublishedMessages => _publishedMessages;

    /// <summary>
    /// Gets the count of published messages.
    /// </summary>
    public int PublishCount => _publishedMessages.Count;

    public Task PublishAsync(T message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _publishedMessages.Add(message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all captured messages.
    /// </summary>
    public void Clear() => _publishedMessages.Clear();
}
