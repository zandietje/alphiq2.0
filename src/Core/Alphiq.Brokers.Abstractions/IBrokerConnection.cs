namespace Alphiq.Brokers.Abstractions;

/// <summary>
/// Broker connection lifecycle management.
/// </summary>
public interface IBrokerConnection
{
    /// <summary>
    /// Gets whether the connection is established.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the broker.
    /// </summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Disconnects from the broker.
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Fires when connection state changes.
    /// </summary>
    event EventHandler<bool>? ConnectionStateChanged;
}
