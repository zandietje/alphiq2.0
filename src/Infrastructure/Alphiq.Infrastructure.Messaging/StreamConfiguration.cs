namespace Alphiq.Infrastructure.Messaging;

/// <summary>
/// Defines the NATS JetStream streams, subjects, and consumer names for Alphiq messaging.
/// </summary>
public static class StreamConfiguration
{
    // Stream names (uppercase with underscores by NATS convention)
    public const string BacktestJobsStream = "ALPHIQ_BACKTEST_JOBS";
    public const string BacktestResultsStream = "ALPHIQ_BACKTEST_RESULTS";
    public const string TradeEventsStream = "ALPHIQ_TRADE_EVENTS";
    public const string EngineStatusStream = "ALPHIQ_ENGINE_STATUS";

    // Subjects (lowercase with dots by NATS convention)
    public const string BacktestJobsSubject = "alphiq.backtest.jobs";
    public const string BacktestResultsSubject = "alphiq.backtest.results";
    public const string TradeEventsSubject = "alphiq.trade.events";
    public const string EngineStatusSubject = "alphiq.engine.status";

    // Consumer names (for durable subscriptions)
    public const string BacktestJobsConsumer = "backtest-worker";
    public const string BacktestResultsConsumer = "results-processor";
    public const string TradeEventsConsumer = "trade-events-processor";
    public const string EngineStatusConsumer = "engine-status-processor";
}
