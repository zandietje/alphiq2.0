using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Alphiq.Infrastructure.Messaging;

/// <summary>
/// Hosted service that initializes NATS JetStream streams on application startup.
/// Streams must exist before messages can be published or consumed.
/// </summary>
public sealed class NatsStreamInitializer : IHostedService
{
    private readonly INatsConnection _connection;
    private readonly ILogger<NatsStreamInitializer> _logger;

    public NatsStreamInitializer(INatsConnection connection, ILogger<NatsStreamInitializer> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var js = new NatsJSContext((NatsConnection)_connection);

        // Create all required streams
        await CreateStreamIfNotExistsAsync(
            js,
            StreamConfiguration.BacktestJobsStream,
            [StreamConfiguration.BacktestJobsSubject],
            cancellationToken);

        await CreateStreamIfNotExistsAsync(
            js,
            StreamConfiguration.BacktestResultsStream,
            [StreamConfiguration.BacktestResultsSubject],
            cancellationToken);

        await CreateStreamIfNotExistsAsync(
            js,
            StreamConfiguration.TradeEventsStream,
            [StreamConfiguration.TradeEventsSubject],
            cancellationToken);

        await CreateStreamIfNotExistsAsync(
            js,
            StreamConfiguration.EngineStatusStream,
            [StreamConfiguration.EngineStatusSubject],
            cancellationToken);

        _logger.LogInformation("NATS JetStream streams initialized successfully");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task CreateStreamIfNotExistsAsync(NatsJSContext js, string streamName, string[] subjects, CancellationToken cancellationToken)
    {
        try
        {
            await js.CreateStreamAsync(new StreamConfig(streamName, subjects), cancellationToken);

            _logger.LogDebug("Created JetStream stream {StreamName}", streamName);
        }
        catch (NatsJSApiException ex) when (ex.Error.ErrCode == 10058)
        {
            // Error code 10058 = stream name already in use
            _logger.LogDebug("JetStream stream {StreamName} already exists", streamName);
        }
    }
}
