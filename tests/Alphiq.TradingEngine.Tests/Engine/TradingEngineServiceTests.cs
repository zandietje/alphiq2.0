using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Alphiq.Brokers.Abstractions;
using Alphiq.Domain.Entities;
using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;
using Alphiq.TradingEngine.Adapters;
using Alphiq.TradingEngine.Engine;
using Alphiq.TradingEngine.Strategies;

namespace Alphiq.TradingEngine.Tests.Engine;

public class TradingEngineServiceTests
{
    private readonly InMemoryMarketDataFeed _marketData;
    private readonly InMemoryOrderExecution _orderExecution;
    private readonly TestClock _clock;
    private readonly InMemoryEngineEventSink _eventSink;
    private readonly TradingEngineService _sut;

    private static readonly SymbolId EurusdSymbolId = new(1);
    private static readonly SymbolId GbpusdSymbolId = new(2);
    private static readonly SymbolId UnknownSymbolId = new(999);

    public TradingEngineServiceTests()
    {
        _clock = new TestClock(DateTimeOffset.Parse("2024-01-15T10:00:00Z"));
        _marketData = new InMemoryMarketDataFeed();
        _orderExecution = new InMemoryOrderExecution(_clock);
        _eventSink = new InMemoryEngineEventSink();

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

    [Fact]
    public void RegisterStrategy_ShouldIncrementCount()
    {
        var strategy = new BuyOnFirstBarStrategy();

        _sut.RegisterStrategy(strategy);

        _sut.StrategyCount.Should().Be(1);
    }

    [Fact]
    public async Task OnBarClosed_HappyPath_ShouldPlaceOrderAndPublishEvent()
    {
        // Arrange
        var strategy = new BuyOnFirstBarStrategy(Timeframe.M5);
        _sut.RegisterStrategy(strategy);

        var bar = CreateBar(EurusdSymbolId, Timeframe.M5, _clock.UtcNow.ToUnixTimeSeconds());

        // Act
        await _sut.OnBarClosedAsync(bar);

        // Assert
        _orderExecution.PlacedOrders.Should().HaveCount(1);
        var order = _orderExecution.PlacedOrders[0];
        order.Side.Should().Be(OrderSide.Buy);
        order.SymbolId.Should().Be(EurusdSymbolId);
        order.Volume.Value.Should().Be(0.01);
        order.Status.Should().Be(OrderStatus.Filled);

        _eventSink.PublishedOrders.Should().HaveCount(1);
        _eventSink.StatusMessages.Should().HaveCount(1);
        _eventSink.StatusMessages[0].Should().Contain("Order placed");
    }

    [Fact]
    public async Task OnBarClosed_StrategyReturnsNoSignal_ShouldNotPlaceOrder()
    {
        // Arrange
        var strategy = new BuyOnFirstBarStrategy(Timeframe.M5);
        _sut.RegisterStrategy(strategy);

        var timestamp = _clock.UtcNow.ToUnixTimeSeconds();
        var bar1 = CreateBar(EurusdSymbolId, Timeframe.M5, timestamp);
        var bar2 = CreateBar(EurusdSymbolId, Timeframe.M5, timestamp + 300);

        // Act - First bar triggers, second should not
        await _sut.OnBarClosedAsync(bar1);
        await _sut.OnBarClosedAsync(bar2);

        // Assert - Only one order should be placed
        _orderExecution.PlacedOrders.Should().HaveCount(1);
        _eventSink.PublishedOrders.Should().HaveCount(1);
    }

    [Fact]
    public async Task OnBarClosed_WrongTimeframe_ShouldNotEvaluateStrategy()
    {
        // Arrange
        var strategy = new BuyOnFirstBarStrategy(Timeframe.M5); // Strategy uses M5
        _sut.RegisterStrategy(strategy);

        var bar = CreateBar(EurusdSymbolId, Timeframe.H1, _clock.UtcNow.ToUnixTimeSeconds()); // But we send H1

        // Act
        await _sut.OnBarClosedAsync(bar);

        // Assert - No order should be placed because timeframe doesn't match
        _orderExecution.PlacedOrders.Should().BeEmpty();
        _eventSink.PublishedOrders.Should().BeEmpty();
    }

    [Fact]
    public async Task OnBarClosed_MultipleStrategies_ShouldEvaluateAll()
    {
        // Arrange
        var strategy1 = new BuyOnFirstBarStrategy(Timeframe.M5);
        var strategy2 = new BuyOnFirstBarStrategy(Timeframe.M5);
        _sut.RegisterStrategy(strategy1);
        _sut.RegisterStrategy(strategy2);

        var bar = CreateBar(EurusdSymbolId, Timeframe.M5, _clock.UtcNow.ToUnixTimeSeconds());

        // Act
        await _sut.OnBarClosedAsync(bar);

        // Assert - Both strategies should trigger orders
        _orderExecution.PlacedOrders.Should().HaveCount(2);
        _eventSink.PublishedOrders.Should().HaveCount(2);
    }

    [Fact]
    public async Task OnBarClosed_CachesBarData()
    {
        // Arrange
        var bar = CreateBar(EurusdSymbolId, Timeframe.M5, _clock.UtcNow.ToUnixTimeSeconds());

        // Act
        await _sut.OnBarClosedAsync(bar);

        // Assert
        _sut.GetCachedBarCount(EurusdSymbolId, Timeframe.M5).Should().Be(1);
    }

    [Fact]
    public async Task OnBarClosed_MultipleTimeframes_ShouldCacheSeparately()
    {
        // Arrange
        var timestamp = _clock.UtcNow.ToUnixTimeSeconds();
        var barM5 = CreateBar(EurusdSymbolId, Timeframe.M5, timestamp);
        var barH1 = CreateBar(EurusdSymbolId, Timeframe.H1, timestamp);

        // Act
        await _sut.OnBarClosedAsync(barM5);
        await _sut.OnBarClosedAsync(barH1);

        // Assert
        _sut.GetCachedBarCount(EurusdSymbolId, Timeframe.M5).Should().Be(1);
        _sut.GetCachedBarCount(EurusdSymbolId, Timeframe.H1).Should().Be(1);
    }

    [Fact]
    public async Task OnBarClosed_CacheEviction_ShouldNotExceedMaxBars()
    {
        // Arrange
        var baseTimestamp = _clock.UtcNow.ToUnixTimeSeconds();

        // Act - Add more than 1000 bars (cache limit)
        for (int i = 0; i < 1005; i++)
        {
            var bar = CreateBar(EurusdSymbolId, Timeframe.M5, baseTimestamp + (i * 300));
            await _sut.OnBarClosedAsync(bar);
        }

        // Assert - Cache should be capped at 1000
        _sut.GetCachedBarCount(EurusdSymbolId, Timeframe.M5).Should().Be(1000);
    }

    [Fact]
    public void GetCachedBarCount_NoData_ShouldReturnZero()
    {
        var count = _sut.GetCachedBarCount(UnknownSymbolId, Timeframe.M5);

        count.Should().Be(0);
    }

    [Fact]
    public async Task OnBarClosed_MultipleSymbols_ShouldCacheSeparately()
    {
        // Arrange
        var timestamp = _clock.UtcNow.ToUnixTimeSeconds();
        var bar1 = CreateBar(EurusdSymbolId, Timeframe.M5, timestamp);
        var bar2 = CreateBar(GbpusdSymbolId, Timeframe.M5, timestamp);

        // Act
        await _sut.OnBarClosedAsync(bar1);
        await _sut.OnBarClosedAsync(bar2);

        // Assert
        _sut.GetCachedBarCount(EurusdSymbolId, Timeframe.M5).Should().Be(1);
        _sut.GetCachedBarCount(GbpusdSymbolId, Timeframe.M5).Should().Be(1);
    }

    private static Bar CreateBar(SymbolId symbolId, Timeframe timeframe, long timestamp)
    {
        return new Bar
        {
            SymbolId = symbolId,
            Timeframe = timeframe,
            Timestamp = timestamp,
            Open = 1.1000,
            High = 1.1050,
            Low = 1.0950,
            Close = 1.1025,
            Volume = 1000
        };
    }
}
