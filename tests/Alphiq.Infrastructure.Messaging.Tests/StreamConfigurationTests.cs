using FluentAssertions;
using Xunit;

namespace Alphiq.Infrastructure.Messaging.Tests;

public class StreamConfigurationTests
{
    [Fact]
    public void StreamNames_FollowNatsConvention()
    {
        // Stream names should be uppercase with underscores
        StreamConfiguration.BacktestJobsStream.Should().Be("ALPHIQ_BACKTEST_JOBS");
        StreamConfiguration.BacktestResultsStream.Should().Be("ALPHIQ_BACKTEST_RESULTS");
        StreamConfiguration.TradeEventsStream.Should().Be("ALPHIQ_TRADE_EVENTS");
        StreamConfiguration.EngineStatusStream.Should().Be("ALPHIQ_ENGINE_STATUS");
    }

    [Fact]
    public void Subjects_FollowNatsConvention()
    {
        // Subjects should be lowercase with dots
        StreamConfiguration.BacktestJobsSubject.Should().Be("alphiq.backtest.jobs");
        StreamConfiguration.BacktestResultsSubject.Should().Be("alphiq.backtest.results");
        StreamConfiguration.TradeEventsSubject.Should().Be("alphiq.trade.events");
        StreamConfiguration.EngineStatusSubject.Should().Be("alphiq.engine.status");
    }

    [Fact]
    public void ConsumerNames_AreDefined()
    {
        StreamConfiguration.BacktestJobsConsumer.Should().NotBeNullOrEmpty();
        StreamConfiguration.BacktestResultsConsumer.Should().NotBeNullOrEmpty();
        StreamConfiguration.TradeEventsConsumer.Should().NotBeNullOrEmpty();
        StreamConfiguration.EngineStatusConsumer.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void StreamNames_AreUnique()
    {
        var streamNames = new[]
        {
            StreamConfiguration.BacktestJobsStream,
            StreamConfiguration.BacktestResultsStream,
            StreamConfiguration.TradeEventsStream,
            StreamConfiguration.EngineStatusStream
        };

        streamNames.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Subjects_AreUnique()
    {
        var subjects = new[]
        {
            StreamConfiguration.BacktestJobsSubject,
            StreamConfiguration.BacktestResultsSubject,
            StreamConfiguration.TradeEventsSubject,
            StreamConfiguration.EngineStatusSubject
        };

        subjects.Should().OnlyHaveUniqueItems();
    }
}
