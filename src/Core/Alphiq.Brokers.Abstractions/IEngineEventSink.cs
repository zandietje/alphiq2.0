using Alphiq.Domain.Entities;

namespace Alphiq.Brokers.Abstractions;

/// <summary>
/// Event publishing sink for engine events.
/// </summary>
public interface IEngineEventSink
{
    Task PublishTradeAsync(Trade trade, CancellationToken ct = default);
    Task PublishOrderAsync(Order order, CancellationToken ct = default);
    Task PublishPositionAsync(Position position, CancellationToken ct = default);
    Task PublishEngineStatusAsync(string status, CancellationToken ct = default);
}
