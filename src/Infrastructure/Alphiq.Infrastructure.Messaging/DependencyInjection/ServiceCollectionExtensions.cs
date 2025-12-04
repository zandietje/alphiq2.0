using Alphiq.Contracts;
using Alphiq.Infrastructure.Messaging.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Alphiq.Infrastructure.Messaging.DependencyInjection;

/// <summary>
/// Extension methods for registering NATS messaging services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers NATS messaging infrastructure with JetStream support.
    /// Requires Aspire's AddNatsClient("nats") to be called first to provide INatsConnection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNatsMessaging(this IServiceCollection services)
    {
        // Register stream initializer to create streams on startup
        services.AddHostedService<NatsStreamInitializer>();

        // Register typed publishers for each message type
        services.AddSingleton<INatsPublisher<BacktestJob>>(sp =>
            new NatsPublisher<BacktestJob>(
                sp.GetRequiredService<INatsConnection>(),
                StreamConfiguration.BacktestJobsSubject,
                sp.GetRequiredService<ILogger<NatsPublisher<BacktestJob>>>()));

        services.AddSingleton<INatsPublisher<BacktestResult>>(sp =>
            new NatsPublisher<BacktestResult>(
                sp.GetRequiredService<INatsConnection>(),
                StreamConfiguration.BacktestResultsSubject,
                sp.GetRequiredService<ILogger<NatsPublisher<BacktestResult>>>()));

        services.AddSingleton<INatsPublisher<TradeEvent>>(sp =>
            new NatsPublisher<TradeEvent>(
                sp.GetRequiredService<INatsConnection>(),
                StreamConfiguration.TradeEventsSubject,
                sp.GetRequiredService<ILogger<NatsPublisher<TradeEvent>>>()));

        services.AddSingleton<INatsPublisher<EngineStatusEvent>>(sp =>
            new NatsPublisher<EngineStatusEvent>(
                sp.GetRequiredService<INatsConnection>(),
                StreamConfiguration.EngineStatusSubject,
                sp.GetRequiredService<ILogger<NatsPublisher<EngineStatusEvent>>>()));

        // Register typed subscribers for each message type
        services.AddSingleton<INatsSubscriber<BacktestJob>>(sp =>
            new NatsSubscriber<BacktestJob>(
                sp.GetRequiredService<INatsConnection>(),
                StreamConfiguration.BacktestJobsStream,
                StreamConfiguration.BacktestJobsConsumer,
                sp.GetRequiredService<ILogger<NatsSubscriber<BacktestJob>>>()));

        services.AddSingleton<INatsSubscriber<BacktestResult>>(sp =>
            new NatsSubscriber<BacktestResult>(
                sp.GetRequiredService<INatsConnection>(),
                StreamConfiguration.BacktestResultsStream,
                StreamConfiguration.BacktestResultsConsumer,
                sp.GetRequiredService<ILogger<NatsSubscriber<BacktestResult>>>()));

        services.AddSingleton<INatsSubscriber<TradeEvent>>(sp =>
            new NatsSubscriber<TradeEvent>(
                sp.GetRequiredService<INatsConnection>(),
                StreamConfiguration.TradeEventsStream,
                StreamConfiguration.TradeEventsConsumer,
                sp.GetRequiredService<ILogger<NatsSubscriber<TradeEvent>>>()));

        services.AddSingleton<INatsSubscriber<EngineStatusEvent>>(sp =>
            new NatsSubscriber<EngineStatusEvent>(
                sp.GetRequiredService<INatsConnection>(),
                StreamConfiguration.EngineStatusStream,
                StreamConfiguration.EngineStatusConsumer,
                sp.GetRequiredService<ILogger<NatsSubscriber<EngineStatusEvent>>>()));

        return services;
    }

    /// <summary>
    /// Registers in-memory NATS adapters for unit testing.
    /// Use this instead of AddNatsMessaging() when testing services that depend on messaging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryNatsMessaging(this IServiceCollection services)
    {
        // BacktestJob
        services.AddSingleton<InMemoryNatsPublisher<BacktestJob>>();
        services.AddSingleton<INatsPublisher<BacktestJob>>(sp =>
            sp.GetRequiredService<InMemoryNatsPublisher<BacktestJob>>());
        services.AddSingleton<InMemoryNatsSubscriber<BacktestJob>>();
        services.AddSingleton<INatsSubscriber<BacktestJob>>(sp =>
            sp.GetRequiredService<InMemoryNatsSubscriber<BacktestJob>>());

        // BacktestResult
        services.AddSingleton<InMemoryNatsPublisher<BacktestResult>>();
        services.AddSingleton<INatsPublisher<BacktestResult>>(sp =>
            sp.GetRequiredService<InMemoryNatsPublisher<BacktestResult>>());
        services.AddSingleton<InMemoryNatsSubscriber<BacktestResult>>();
        services.AddSingleton<INatsSubscriber<BacktestResult>>(sp =>
            sp.GetRequiredService<InMemoryNatsSubscriber<BacktestResult>>());

        // TradeEvent
        services.AddSingleton<InMemoryNatsPublisher<TradeEvent>>();
        services.AddSingleton<INatsPublisher<TradeEvent>>(sp =>
            sp.GetRequiredService<InMemoryNatsPublisher<TradeEvent>>());
        services.AddSingleton<InMemoryNatsSubscriber<TradeEvent>>();
        services.AddSingleton<INatsSubscriber<TradeEvent>>(sp =>
            sp.GetRequiredService<InMemoryNatsSubscriber<TradeEvent>>());

        // EngineStatusEvent
        services.AddSingleton<InMemoryNatsPublisher<EngineStatusEvent>>();
        services.AddSingleton<INatsPublisher<EngineStatusEvent>>(sp =>
            sp.GetRequiredService<InMemoryNatsPublisher<EngineStatusEvent>>());
        services.AddSingleton<InMemoryNatsSubscriber<EngineStatusEvent>>();
        services.AddSingleton<INatsSubscriber<EngineStatusEvent>>(sp =>
            sp.GetRequiredService<InMemoryNatsSubscriber<EngineStatusEvent>>());

        return services;
    }
}
