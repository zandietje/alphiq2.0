using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Alphiq.Infrastructure.Messaging;

/// <summary>
/// NATS JetStream publisher implementation.
/// Publishes messages to a configured subject with guaranteed delivery.
/// </summary>
/// <typeparam name="T">The message type to publish.</typeparam>
public sealed class NatsPublisher<T> : INatsPublisher<T> where T : class
{
    private readonly INatsConnection _connection;
    private readonly string _subject;
    private readonly ILogger<NatsPublisher<T>> _logger;
    private NatsJSContext? _jsContext;

    public NatsPublisher(INatsConnection connection, string subject, ILogger<NatsPublisher<T>> logger)
    {
        _connection = connection;
        _subject = subject;
        _logger = logger;
    }

    public async Task PublishAsync(T message, CancellationToken cancellationToken = default)
    {
        // Lazy init JetStream context (connection might not be ready at construction time)
        _jsContext ??= new NatsJSContext((NatsConnection)_connection);

        var ack = await _jsContext.PublishAsync(subject: _subject, data: message, cancellationToken: cancellationToken);

        // Ensure the message was acknowledged by the server
        ack.EnsureSuccess();

        _logger.LogDebug("Published {MessageType} to {Subject}", typeof(T).Name, _subject);
    }
}
