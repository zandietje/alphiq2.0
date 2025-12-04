using Alphiq.Brokers.Abstractions;
using Alphiq.Domain.Entities;

namespace Alphiq.Backtest.Worker;

/// <summary>
/// Null implementation of IEngineEventSink for backtesting.
/// Events are not published anywhere during backtests.
/// </summary>
public sealed class NullEngineEventSink : IEngineEventSink
{
    public Task PublishTradeAsync(Trade trade, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishOrderAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishPositionAsync(Position position, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishEngineStatusAsync(string status, CancellationToken ct = default) => Task.CompletedTask;
}
