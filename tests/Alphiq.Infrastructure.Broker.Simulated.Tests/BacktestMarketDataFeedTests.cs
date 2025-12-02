using FluentAssertions;
using Xunit;
using Alphiq.Domain.Entities;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.Infrastructure.Broker.Simulated.Tests;

public class BacktestMarketDataFeedTests
{
    private readonly BacktestClock _clock;
    private readonly BacktestMarketDataFeed _sut;
    private static readonly SymbolId TestSymbolId = new(1);

    public BacktestMarketDataFeedTests()
    {
        _clock = new BacktestClock();
        _sut = new BacktestMarketDataFeed(_clock);
    }

    #region LoadBars Tests

    [Fact]
    public void LoadBars_StoresBarsForRetrieval()
    {
        var bars = CreateBars(5, 1705315200);
        _sut.LoadBars(TestSymbolId, Timeframe.M5, bars);

        var result = _sut.GetAllBars(TestSymbolId, Timeframe.M5);

        result.Should().HaveCount(5);
    }

    [Fact]
    public void LoadBars_SortsByTimestamp()
    {
        var bars = new[]
        {
            CreateBar(TestSymbolId, 1705315800), // Third
            CreateBar(TestSymbolId, 1705315200), // First
            CreateBar(TestSymbolId, 1705315500), // Second
        };
        _sut.LoadBars(TestSymbolId, Timeframe.M5, bars);

        var result = _sut.GetAllBars(TestSymbolId, Timeframe.M5);

        result[0].Timestamp.Should().Be(1705315200);
        result[1].Timestamp.Should().Be(1705315500);
        result[2].Timestamp.Should().Be(1705315800);
    }

    [Fact]
    public void LoadBars_AccumulatesMultipleCalls()
    {
        _sut.LoadBars(TestSymbolId, Timeframe.M5, CreateBars(3, 1705315200));
        _sut.LoadBars(TestSymbolId, Timeframe.M5, CreateBars(2, 1705316100));

        var result = _sut.GetAllBars(TestSymbolId, Timeframe.M5);

        result.Should().HaveCount(5);
    }

    [Fact]
    public void LoadBars_SeparatesBySymbol()
    {
        var symbolA = new SymbolId(1);
        var symbolB = new SymbolId(2);

        _sut.LoadBars(symbolA, Timeframe.M5, CreateBars(3, 1705315200, symbolA));
        _sut.LoadBars(symbolB, Timeframe.M5, CreateBars(2, 1705315200, symbolB));

        _sut.GetAllBars(symbolA, Timeframe.M5).Should().HaveCount(3);
        _sut.GetAllBars(symbolB, Timeframe.M5).Should().HaveCount(2);
    }

    [Fact]
    public void LoadBars_SeparatesByTimeframe()
    {
        _sut.LoadBars(TestSymbolId, Timeframe.M5, CreateBars(3, 1705315200));
        _sut.LoadBars(TestSymbolId, Timeframe.H1, CreateBars(2, 1705315200));

        _sut.GetAllBars(TestSymbolId, Timeframe.M5).Should().HaveCount(3);
        _sut.GetAllBars(TestSymbolId, Timeframe.H1).Should().HaveCount(2);
    }

    #endregion

    #region GetAllBars Tests

    [Fact]
    public void GetAllBars_ReturnsEmptyForUnknownSymbol()
    {
        var result = _sut.GetAllBars(new SymbolId(999), Timeframe.M5);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAllBars_ReturnsEmptyForUnknownTimeframe()
    {
        _sut.LoadBars(TestSymbolId, Timeframe.M5, CreateBars(3, 1705315200));

        var result = _sut.GetAllBars(TestSymbolId, Timeframe.H1);

        result.Should().BeEmpty();
    }

    #endregion

    #region SubscribeBarsAsync Tests

    [Fact]
    public async Task SubscribeBarsAsync_YieldsBarsChronologically()
    {
        var bars = CreateBars(5, 1705315200);
        _sut.LoadBars(TestSymbolId, Timeframe.M5, bars);

        var yielded = new List<Bar>();
        await foreach (var bar in _sut.SubscribeBarsAsync(TestSymbolId, Timeframe.M5))
        {
            yielded.Add(bar);
        }

        yielded.Should().HaveCount(5);
        for (int i = 1; i < yielded.Count; i++)
        {
            yielded[i].Timestamp.Should().BeGreaterThan(yielded[i - 1].Timestamp);
        }
    }

    [Fact]
    public async Task SubscribeBarsAsync_AdvancesClockToEachBarClose()
    {
        var bars = CreateBars(3, 1705315200);
        _sut.LoadBars(TestSymbolId, Timeframe.M5, bars);

        var clockTimes = new List<DateTimeOffset>();
        await foreach (var bar in _sut.SubscribeBarsAsync(TestSymbolId, Timeframe.M5))
        {
            clockTimes.Add(_clock.UtcNow);
        }

        clockTimes[0].Should().Be(bars[0].DateTime);
        clockTimes[1].Should().Be(bars[1].DateTime);
        clockTimes[2].Should().Be(bars[2].DateTime);
    }

    [Fact]
    public async Task SubscribeBarsAsync_YieldsNothingForUnknownSymbol()
    {
        var yielded = new List<Bar>();
        await foreach (var bar in _sut.SubscribeBarsAsync(new SymbolId(999), Timeframe.M5))
        {
            yielded.Add(bar);
        }

        yielded.Should().BeEmpty();
    }

    [Fact]
    public async Task SubscribeBarsAsync_RespecsCancellation()
    {
        var bars = CreateBars(100, 1705315200);
        _sut.LoadBars(TestSymbolId, Timeframe.M5, bars);

        var cts = new CancellationTokenSource();
        var yielded = new List<Bar>();

        await foreach (var bar in _sut.SubscribeBarsAsync(TestSymbolId, Timeframe.M5, cts.Token))
        {
            yielded.Add(bar);
            if (yielded.Count == 5)
                cts.Cancel();
        }

        yielded.Should().HaveCount(5);
    }

    #endregion

    #region GetHistoryAsync Tests

    [Fact]
    public async Task GetHistoryAsync_FiltersByDateRange()
    {
        var bars = CreateBars(10, 1705315200); // 10 bars starting at a specific timestamp
        _sut.LoadBars(TestSymbolId, Timeframe.M5, bars);

        var from = DateTimeOffset.FromUnixTimeSeconds(1705315500);
        var to = DateTimeOffset.FromUnixTimeSeconds(1705316000);

        var result = await _sut.GetHistoryAsync(TestSymbolId, Timeframe.M5, from, to, default);

        // Should include bars with timestamps between from and to
        result.Should().AllSatisfy(b =>
        {
            b.DateTime.Should().BeOnOrAfter(from);
            b.DateTime.Should().BeOnOrBefore(to);
        });
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsEmptyForUnknownSymbol()
    {
        var from = DateTimeOffset.Parse("2024-01-15T10:00:00Z");
        var to = DateTimeOffset.Parse("2024-01-15T12:00:00Z");

        var result = await _sut.GetHistoryAsync(new SymbolId(999), Timeframe.M5, from, to, default);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsOrderedByTimestamp()
    {
        var bars = CreateBars(5, 1705315200);
        _sut.LoadBars(TestSymbolId, Timeframe.M5, bars);

        var from = DateTimeOffset.FromUnixTimeSeconds(1705315000);
        var to = DateTimeOffset.FromUnixTimeSeconds(1705316500);

        var result = await _sut.GetHistoryAsync(TestSymbolId, Timeframe.M5, from, to, default);

        for (int i = 1; i < result.Count; i++)
        {
            result[i].Timestamp.Should().BeGreaterThan(result[i - 1].Timestamp);
        }
    }

    #endregion

    #region SubscribeTicksAsync Tests

    [Fact]
    public async Task SubscribeTicksAsync_YieldsNothing()
    {
        // Tick-based backtesting is not implemented
        var yielded = new List<Tick>();
        await foreach (var tick in _sut.SubscribeTicksAsync(TestSymbolId))
        {
            yielded.Add(tick);
        }

        yielded.Should().BeEmpty();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllBars()
    {
        _sut.LoadBars(TestSymbolId, Timeframe.M5, CreateBars(5, 1705315200));
        _sut.LoadBars(new SymbolId(2), Timeframe.H1, CreateBars(3, 1705315200));

        _sut.Clear();

        _sut.GetAllBars(TestSymbolId, Timeframe.M5).Should().BeEmpty();
        _sut.GetAllBars(new SymbolId(2), Timeframe.H1).Should().BeEmpty();
    }

    #endregion

    private static Bar[] CreateBars(int count, long startTimestamp, SymbolId? symbolId = null)
    {
        var bars = new Bar[count];
        var symbol = symbolId ?? TestSymbolId;
        for (int i = 0; i < count; i++)
        {
            bars[i] = CreateBar(symbol, startTimestamp + (i * 300)); // 5 min intervals
        }
        return bars;
    }

    private static Bar CreateBar(SymbolId symbolId, long timestamp)
    {
        return new Bar
        {
            SymbolId = symbolId,
            Timeframe = Timeframe.M5,
            Timestamp = timestamp,
            Open = 1.1000 + (timestamp % 100) * 0.0001,
            High = 1.1050 + (timestamp % 100) * 0.0001,
            Low = 1.0950 + (timestamp % 100) * 0.0001,
            Close = 1.1025 + (timestamp % 100) * 0.0001,
            Volume = 1000
        };
    }
}
