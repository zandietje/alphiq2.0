using Microsoft.Extensions.Logging;
using Alphiq.Contracts;
using Alphiq.Domain.Entities;
using Alphiq.Domain.ValueObjects;
using Alphiq.Infrastructure.Broker.Simulated;
using Alphiq.Infrastructure.Supabase.Repositories;
using Alphiq.TradingEngine.Engine;
using Alphiq.TradingEngine.Strategies;

namespace Alphiq.Backtest.Worker;

/// <summary>
/// Orchestrates backtest execution by coordinating simulated components,
/// market data, and the trading engine.
/// </summary>
public sealed class BacktestOrchestrator
{
    private readonly ICandleRepository _candleRepository;
    private readonly IStrategyFactory _strategyFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<BacktestOrchestrator> _logger;

    private const decimal InitialBalance = 10000m;

    public BacktestOrchestrator(
        ICandleRepository candleRepository,
        IStrategyFactory strategyFactory,
        ILoggerFactory loggerFactory)
    {
        _candleRepository = candleRepository;
        _strategyFactory = strategyFactory;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<BacktestOrchestrator>();
    }

    /// <summary>
    /// Runs a backtest for the given job configuration.
    /// </summary>
    public async Task<BacktestResult> RunAsync(BacktestJob job, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Starting backtest {JobId} for strategy {Strategy} v{Version}",
            job.JobId, job.StrategyName, job.StrategyVersion);

        try
        {
            // Create strategy
            var strategy = _strategyFactory.CreateByName(job.StrategyName);
            if (strategy is null)
            {
                return CreateErrorResult(job, $"Unknown strategy: {job.StrategyName}");
            }

            // Initialize simulated components
            // Start clock at MinValue - it will advance to the first bar's timestamp
            var clock = new BacktestClock();
            var orderExecution = new SimulatedOrderExecution(clock);
            var marketDataFeed = new BacktestMarketDataFeed(clock);
            var eventSink = new NullEngineEventSink();

            // Create trading engine
            var engine = new TradingEngineService(
                marketDataFeed,
                orderExecution,
                clock,
                eventSink,
                _loggerFactory.CreateLogger<TradingEngineService>());

            engine.RegisterStrategy(strategy);

            // Load market data for each symbol
            foreach (var symbolId in job.Symbols)
            {
                var bars = await _candleRepository.GetBarsAsync(
                    symbolId,
                    strategy.MainTimeframe,
                    job.StartDate,
                    job.EndDate,
                    ct);

                _logger.LogDebug(
                    "Loaded {Count} bars for symbol {Symbol} ({Timeframe})",
                    bars.Count, symbolId.Value, strategy.MainTimeframe.Code);

                marketDataFeed.LoadBars(symbolId, strategy.MainTimeframe, bars);
            }

            // Run backtest - process each bar chronologically
            var allBars = GetAllBarsChronologically(marketDataFeed, job.Symbols, strategy.MainTimeframe);

            foreach (var bar in allBars)
            {
                ct.ThrowIfCancellationRequested();

                // Advance clock
                clock.AdvanceToBarClose(bar);

                // Process pending orders and SL/TP at bar open (T+1 execution)
                orderExecution.ProcessBar(bar);

                // Feed bar to engine for strategy evaluation
                await engine.OnBarClosedAsync(bar, ct);
            }

            // Calculate results
            var result = CalculateResults(job, orderExecution, InitialBalance);

            _logger.LogInformation(
                "Completed backtest {JobId}: {Trades} trades, {WinRate:P1} win rate, PF={ProfitFactor:F2}",
                job.JobId, result.TotalTrades, result.WinRate, result.ProfitFactor);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Backtest {JobId} was cancelled", job.JobId);
            return CreateErrorResult(job, "Backtest cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backtest {JobId} failed", job.JobId);
            return CreateErrorResult(job, ex.Message);
        }
    }

    private static IReadOnlyList<Bar> GetAllBarsChronologically(
        BacktestMarketDataFeed feed,
        IReadOnlyList<SymbolId> symbols,
        Timeframe timeframe)
    {
        return symbols
            .SelectMany(s => feed.GetAllBars(s, timeframe))
            .OrderBy(b => b.Timestamp)
            .ToList();
    }

    private static BacktestResult CalculateResults(
        BacktestJob job,
        SimulatedOrderExecution execution,
        decimal initialBalance)
    {
        var trades = execution.Trades;
        var closedPositions = execution.ClosedPositions;

        // Calculate P&L from closed positions
        decimal totalPnl = 0m;
        decimal totalGrossProfit = 0m;
        decimal totalGrossLoss = 0m;
        int winningTrades = 0;
        int losingTrades = 0;

        // Track equity curve for drawdown calculation
        var equityCurve = new List<decimal> { initialBalance };
        decimal currentEquity = initialBalance;
        decimal peakEquity = initialBalance;
        decimal maxDrawdown = 0m;

        // Group trades by position (entry + exit pairs)
        var tradesByPosition = trades
            .GroupBy(t => t.OrderId)
            .Where(g => g.Count() >= 2)
            .ToList();

        foreach (var positionTrades in tradesByPosition)
        {
            var orderedTrades = positionTrades.OrderBy(t => t.ExecutedAt).ToList();
            if (orderedTrades.Count < 2) continue;

            var entryTrade = orderedTrades[0];
            var exitTrade = orderedTrades[1];

            // Calculate P&L
            decimal pnl;
            if (entryTrade.Side == Domain.Enums.OrderSide.Buy)
            {
                pnl = (decimal)(exitTrade.Price - entryTrade.Price) * (decimal)entryTrade.Volume.Value;
            }
            else
            {
                pnl = (decimal)(entryTrade.Price - exitTrade.Price) * (decimal)entryTrade.Volume.Value;
            }

            // Subtract commissions
            pnl -= entryTrade.Commission.Amount + exitTrade.Commission.Amount;

            totalPnl += pnl;
            currentEquity += pnl;
            equityCurve.Add(currentEquity);

            if (pnl > 0)
            {
                winningTrades++;
                totalGrossProfit += pnl;
            }
            else
            {
                losingTrades++;
                totalGrossLoss += Math.Abs(pnl);
            }

            // Update peak and drawdown
            if (currentEquity > peakEquity)
            {
                peakEquity = currentEquity;
            }
            else
            {
                var drawdown = (peakEquity - currentEquity) / peakEquity;
                if (drawdown > maxDrawdown)
                {
                    maxDrawdown = drawdown;
                }
            }
        }

        int totalTrades = winningTrades + losingTrades;
        decimal winRate = totalTrades > 0 ? (decimal)winningTrades / totalTrades : 0m;
        decimal profitFactor = totalGrossLoss > 0 ? totalGrossProfit / totalGrossLoss : 0m;

        return new BacktestResult
        {
            JobId = job.JobId,
            Success = true,
            InitialBalance = initialBalance,
            FinalBalance = initialBalance + totalPnl,
            TotalTrades = totalTrades,
            WinningTrades = winningTrades,
            LosingTrades = losingTrades,
            ProfitFactor = profitFactor,
            MaxDrawdownPercent = maxDrawdown * 100m,
            WinRate = winRate,
            CompletedAt = DateTimeOffset.UtcNow
        };
    }

    private static BacktestResult CreateErrorResult(BacktestJob job, string error)
    {
        return new BacktestResult
        {
            JobId = job.JobId,
            Success = false,
            Error = error,
            InitialBalance = InitialBalance,
            FinalBalance = InitialBalance,
            TotalTrades = 0,
            WinningTrades = 0,
            LosingTrades = 0,
            ProfitFactor = 0m,
            MaxDrawdownPercent = 0m,
            WinRate = 0m,
            CompletedAt = DateTimeOffset.UtcNow
        };
    }
}
