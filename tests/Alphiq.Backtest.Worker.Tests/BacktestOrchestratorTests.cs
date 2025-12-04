using FluentAssertions;
using NSubstitute;
using Xunit;
using Microsoft.Extensions.Logging;
using Alphiq.Contracts;
using Alphiq.Domain.Entities;
using Alphiq.Domain.ValueObjects;
using Alphiq.Infrastructure.Supabase.Repositories;
using Alphiq.TradingEngine.Strategies;
using Alphiq.Backtest.Worker;

namespace Alphiq.Backtest.Worker.Tests;

public class BacktestOrchestratorTests
{
    private readonly ICandleRepository _candleRepository;
    private readonly IStrategyFactory _strategyFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly BacktestOrchestrator _orchestrator;

    private static readonly SymbolId TestSymbolId = new(1);

    public BacktestOrchestratorTests()
    {
        _candleRepository = Substitute.For<ICandleRepository>();
        _strategyFactory = Substitute.For<IStrategyFactory>();

        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        _orchestrator = new BacktestOrchestrator(
            _candleRepository,
            _strategyFactory,
            _loggerFactory);
    }

    [Fact]
    public async Task RunAsync_UnknownStrategy_ReturnsErrorResult()
    {
        // Arrange
        var job = CreateTestJob("UnknownStrategy");
        _strategyFactory.CreateByName("UnknownStrategy").Returns((ISignalStrategy?)null);

        // Act
        var result = await _orchestrator.RunAsync(job);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Unknown strategy");
        result.JobId.Should().Be(job.JobId);
    }

    [Fact]
    public async Task RunAsync_WithBuyOnFirstBarStrategy_ExecutesTrades()
    {
        // Arrange
        var job = CreateTestJob("BuyOnFirstBar");
        var strategy = new BuyOnFirstBarStrategy(Timeframe.M5);
        _strategyFactory.CreateByName("BuyOnFirstBar").Returns(strategy);

        var bars = CreateTestBars(TestSymbolId, Timeframe.M5, 10);
        _candleRepository
            .GetBarsAsync(TestSymbolId, Timeframe.M5, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(bars);

        // Act
        var result = await _orchestrator.RunAsync(job);

        // Assert
        result.Success.Should().BeTrue(because: "Error was: {0}", result.Error ?? "no error");
        result.JobId.Should().Be(job.JobId);
        result.InitialBalance.Should().Be(10000m);
        result.TotalTrades.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task RunAsync_WithCancellation_ReturnsErrorResult()
    {
        // Arrange
        var job = CreateTestJob("BuyOnFirstBar");
        var strategy = new BuyOnFirstBarStrategy(Timeframe.M5);
        _strategyFactory.CreateByName("BuyOnFirstBar").Returns(strategy);

        var bars = CreateTestBars(TestSymbolId, Timeframe.M5, 100);
        _candleRepository
            .GetBarsAsync(Arg.Any<SymbolId>(), Arg.Any<Timeframe>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(bars);

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await _orchestrator.RunAsync(job, cts.Token);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("cancelled");
    }

    [Fact]
    public async Task RunAsync_WithNoMarketData_ReturnsEmptyResult()
    {
        // Arrange
        var job = CreateTestJob("BuyOnFirstBar");
        var strategy = new BuyOnFirstBarStrategy(Timeframe.M5);
        _strategyFactory.CreateByName("BuyOnFirstBar").Returns(strategy);

        _candleRepository
            .GetBarsAsync(Arg.Any<SymbolId>(), Arg.Any<Timeframe>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Bar>());

        // Act
        var result = await _orchestrator.RunAsync(job);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalTrades.Should().Be(0);
        result.FinalBalance.Should().Be(result.InitialBalance);
    }

    [Fact]
    public async Task RunAsync_SetsCorrectMetrics()
    {
        // Arrange
        var job = CreateTestJob("BuyOnFirstBar");
        var strategy = new BuyOnFirstBarStrategy(Timeframe.M5);
        _strategyFactory.CreateByName("BuyOnFirstBar").Returns(strategy);

        var bars = CreateTestBars(TestSymbolId, Timeframe.M5, 5);
        _candleRepository
            .GetBarsAsync(TestSymbolId, Timeframe.M5, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(bars);

        // Act
        var result = await _orchestrator.RunAsync(job);

        // Assert
        result.CompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        result.WinRate.Should().BeInRange(0m, 1m);
        result.MaxDrawdownPercent.Should().BeGreaterThanOrEqualTo(0m);
        result.ProfitFactor.Should().BeGreaterThanOrEqualTo(0m);
        result.WinningTrades.Should().BeGreaterThanOrEqualTo(0);
        result.LosingTrades.Should().BeGreaterThanOrEqualTo(0);
        (result.WinningTrades + result.LosingTrades).Should().Be(result.TotalTrades);
    }

    private static BacktestJob CreateTestJob(string strategyName)
    {
        return new BacktestJob
        {
            JobId = Guid.NewGuid().ToString("N"),
            StrategyName = strategyName,
            StrategyVersion = 1,
            Symbols = new List<SymbolId> { TestSymbolId },
            StartDate = DateTimeOffset.UtcNow.AddDays(-30),
            EndDate = DateTimeOffset.UtcNow,
            Parameters = new Dictionary<string, object>(),
            RequestedAt = DateTimeOffset.UtcNow
        };
    }

    private static IReadOnlyList<Bar> CreateTestBars(SymbolId symbolId, Timeframe timeframe, int count, DateTimeOffset? startDate = null)
    {
        var basePrice = 1.1000;
        // Round to nearest minute to avoid sub-second precision issues with BacktestClock
        var baseDate = startDate ?? DateTimeOffset.UtcNow.AddDays(-30);
        var baseTimestamp = new DateTimeOffset(
            baseDate.Year, baseDate.Month, baseDate.Day,
            baseDate.Hour, baseDate.Minute, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        return Enumerable.Range(0, count)
            .Select(i =>
            {
                var priceVariation = (i % 10 - 5) * 0.001; // Create some price variation
                return new Bar
                {
                    SymbolId = symbolId,
                    Timeframe = timeframe,
                    Timestamp = baseTimestamp + (i * (long)timeframe.Duration.TotalSeconds),
                    Open = basePrice + priceVariation,
                    High = basePrice + priceVariation + 0.002,
                    Low = basePrice + priceVariation - 0.002,
                    Close = basePrice + priceVariation + 0.001,
                    Volume = 1000 + i * 10
                };
            })
            .ToList();
    }
}
