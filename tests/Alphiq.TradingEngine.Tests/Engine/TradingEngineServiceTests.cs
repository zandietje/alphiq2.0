using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using Alphiq.Brokers.Abstractions;
using Alphiq.TradingEngine.Engine;

namespace Alphiq.TradingEngine.Tests.Engine;

public class TradingEngineServiceTests
{
    private readonly IMarketDataFeed _marketData;
    private readonly IOrderExecution _orderExecution;
    private readonly IClock _clock;
    private readonly IEngineEventSink _eventSink;
    private readonly TradingEngineService _sut;

    public TradingEngineServiceTests()
    {
        _marketData = Substitute.For<IMarketDataFeed>();
        _orderExecution = Substitute.For<IOrderExecution>();
        _clock = Substitute.For<IClock>();
        _eventSink = Substitute.For<IEngineEventSink>();

        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        _sut = new TradingEngineService(
            _marketData,
            _orderExecution,
            _clock,
            _eventSink,
            NullLogger<TradingEngineService>.Instance);
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        _sut.Should().NotBeNull();
    }

    // TODO: Add more tests as implementation progresses
}
