using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Alphiq.Contracts;
using Alphiq.Infrastructure.Messaging;

namespace Alphiq.Backtest.Worker;

/// <summary>
/// Background service that subscribes to backtest jobs from NATS,
/// executes them via BacktestOrchestrator, and publishes results.
/// </summary>
public sealed class BacktestWorkerService : BackgroundService
{
    private readonly INatsSubscriber<BacktestJob> _jobSubscriber;
    private readonly INatsPublisher<BacktestResult> _resultPublisher;
    private readonly BacktestOrchestrator _orchestrator;
    private readonly ILogger<BacktestWorkerService> _logger;

    public BacktestWorkerService(
        INatsSubscriber<BacktestJob> jobSubscriber,
        INatsPublisher<BacktestResult> resultPublisher,
        BacktestOrchestrator orchestrator,
        ILogger<BacktestWorkerService> logger)
    {
        _jobSubscriber = jobSubscriber;
        _resultPublisher = resultPublisher;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Backtest worker service starting");

        try
        {
            await foreach (var message in _jobSubscriber.SubscribeAsync(stoppingToken))
            {
                await ProcessJobAsync(message, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Backtest worker service stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backtest worker service encountered an error");
            throw;
        }
    }

    private async Task ProcessJobAsync(NatsMessage<BacktestJob> message, CancellationToken ct)
    {
        var job = message.Data;
        _logger.LogInformation("Received backtest job {JobId} for strategy {Strategy}",
            job.JobId, job.StrategyName);

        try
        {
            // Run the backtest
            var result = await _orchestrator.RunAsync(job, ct);

            // Publish result
            await _resultPublisher.PublishAsync(result, ct);

            // Acknowledge the message
            await message.AckAsync(ct);

            _logger.LogInformation(
                "Completed backtest job {JobId}: Success={Success}, Trades={Trades}",
                job.JobId, result.Success, result.TotalTrades);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process backtest job {JobId}", job.JobId);

            // Publish error result
            var errorResult = new BacktestResult
            {
                JobId = job.JobId,
                Success = false,
                Error = ex.Message,
                InitialBalance = 10000m,
                FinalBalance = 10000m,
                TotalTrades = 0,
                WinningTrades = 0,
                LosingTrades = 0,
                ProfitFactor = 0m,
                MaxDrawdownPercent = 0m,
                WinRate = 0m,
                CompletedAt = DateTimeOffset.UtcNow
            };

            try
            {
                await _resultPublisher.PublishAsync(errorResult, ct);
            }
            catch (Exception pubEx)
            {
                _logger.LogError(pubEx, "Failed to publish error result for job {JobId}", job.JobId);
            }

            // Nak to allow retry
            await message.NakAsync(ct);
        }
    }
}
