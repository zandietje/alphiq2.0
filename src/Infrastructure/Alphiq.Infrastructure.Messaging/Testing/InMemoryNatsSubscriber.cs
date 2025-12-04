using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Alphiq.Infrastructure.Messaging.Testing;

/// <summary>
/// In-memory implementation of INatsSubscriber for unit testing.
/// Allows enqueuing messages and tracks acknowledgments.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public sealed class InMemoryNatsSubscriber<T> : INatsSubscriber<T> where T : class
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();
    private readonly List<T> _acknowledgedMessages = [];
    private readonly List<T> _negativeAcknowledgedMessages = [];

    /// <summary>
    /// Gets the list of messages that have been acknowledged.
    /// </summary>
    public IReadOnlyList<T> AcknowledgedMessages => _acknowledgedMessages;

    /// <summary>
    /// Gets the list of messages that have been negative-acknowledged.
    /// </summary>
    public IReadOnlyList<T> NegativeAcknowledgedMessages => _negativeAcknowledgedMessages;

    /// <summary>
    /// Enqueues a message to be yielded by SubscribeAsync.
    /// </summary>
    public void Enqueue(T message) => _channel.Writer.TryWrite(message);

    /// <summary>
    /// Signals that no more messages will be enqueued.
    /// </summary>
    public void Complete() => _channel.Writer.Complete();

    public async IAsyncEnumerable<NatsMessage<T>> SubscribeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var data in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            var capturedData = data;
            yield return new NatsMessage<T>
            {
                Data = data,
                AckAsync = _ =>
                {
                    _acknowledgedMessages.Add(capturedData);
                    return ValueTask.CompletedTask;
                },
                NakAsync = _ =>
                {
                    _negativeAcknowledgedMessages.Add(capturedData);
                    return ValueTask.CompletedTask;
                }
            };
        }
    }

    /// <summary>
    /// Clears all tracked acknowledgments.
    /// </summary>
    public void ClearAcknowledgments()
    {
        _acknowledgedMessages.Clear();
        _negativeAcknowledgedMessages.Clear();
    }
}
