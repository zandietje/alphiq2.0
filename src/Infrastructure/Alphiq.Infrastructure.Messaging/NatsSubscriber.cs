using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Alphiq.Infrastructure.Messaging;

/// <summary>
/// NATS JetStream subscriber implementation.
/// Subscribes to messages from a durable consumer with acknowledgment support.
/// </summary>
/// <typeparam name="T">The message type to subscribe to.</typeparam>
public sealed class NatsSubscriber<T> : INatsSubscriber<T> where T : class
{
    private readonly INatsConnection _connection;
    private readonly string _streamName;
    private readonly string _consumerName;
    private readonly ILogger<NatsSubscriber<T>> _logger;

    public NatsSubscriber(INatsConnection connection, string streamName, string consumerName, ILogger<NatsSubscriber<T>> logger)
    {
        _connection = connection;
        _streamName = streamName;
        _consumerName = consumerName;
        _logger = logger;
    }

    public async IAsyncEnumerable<NatsMessage<T>> SubscribeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var js = new NatsJSContext((NatsConnection)_connection);

        // Get or create the consumer
        var consumer = await js.CreateOrUpdateConsumerAsync(_streamName, new ConsumerConfig(_consumerName), cancellationToken);

        _logger.LogInformation(
            "Subscribed to stream {Stream} with consumer {Consumer}",
            _streamName,
            _consumerName);

        // Consume messages and wrap them with our NatsMessage type
        await foreach (var msg in consumer.ConsumeAsync<T>(cancellationToken: cancellationToken))
        {
            if (msg.Data is null)
            {
                _logger.LogWarning("Received null message data, skipping");
                await msg.AckAsync(cancellationToken: cancellationToken);
                continue;
            }

            yield return new NatsMessage<T>
            {
                Data = msg.Data,
                AckAsync = ct => msg.AckAsync(cancellationToken: ct),
                NakAsync = ct => msg.NakAsync(cancellationToken: ct)
            };
        }
    }
}
