using Alphiq.Brokers.Abstractions;
using Alphiq.Domain.Entities;
using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;
using Alphiq.Infrastructure.Broker.Simulated.Models;

namespace Alphiq.Infrastructure.Broker.Simulated;

/// <summary>
/// Simulated order execution adapter for backtesting.
/// Implements T+1 execution model: orders placed on bar N fill at bar N+1's OPEN.
/// </summary>
public sealed class SimulatedOrderExecution : IOrderExecution
{
    private readonly BacktestClock _clock;
    private readonly BacktestSettings _settings;
    private readonly List<PendingOrder> _pendingOrders = new();
    private readonly List<SimulatedPosition> _openPositions = new();
    private readonly List<SimulatedPosition> _closedPositions = new();
    private readonly List<Trade> _trades = new();

    public SimulatedOrderExecution(BacktestClock clock, BacktestSettings? settings = null)
    {
        _clock = clock;
        _settings = settings ?? new BacktestSettings();
    }

    /// <summary>
    /// All executed trades (fills).
    /// </summary>
    public IReadOnlyList<Trade> Trades => _trades;

    /// <summary>
    /// Currently open positions.
    /// </summary>
    public IReadOnlyList<SimulatedPosition> OpenPositions => _openPositions;

    /// <summary>
    /// Closed positions (for P&L calculation).
    /// </summary>
    public IReadOnlyList<SimulatedPosition> ClosedPositions => _closedPositions;

    /// <summary>
    /// Pending orders awaiting fill at next bar.
    /// </summary>
    public IReadOnlyList<PendingOrder> PendingOrders => _pendingOrders;

    /// <inheritdoc />
    public Task<Order> PlaceOrderAsync(
        SymbolId symbolId,
        OrderSide side,
        OrderType type,
        Quantity volume,
        double? price = null,
        double? stopLoss = null,
        double? takeProfit = null,
        string? clientOrderId = null,
        CancellationToken ct = default)
    {
        var orderId = Guid.NewGuid().ToString("N")[..8];

        var pending = new PendingOrder
        {
            OrderId = orderId,
            SymbolId = symbolId,
            Side = side,
            Type = type,
            Volume = volume,
            Price = price,
            StopLoss = stopLoss,
            TakeProfit = takeProfit,
            CreatedAt = _clock.UtcNow,
            ClientOrderId = clientOrderId
        };
        _pendingOrders.Add(pending);

        // Return order in Pending state (will fill on next bar)
        var order = new Order
        {
            OrderId = orderId,
            SymbolId = symbolId,
            Side = side,
            Type = type,
            Volume = volume,
            Price = price,
            StopLoss = stopLoss,
            TakeProfit = takeProfit,
            Status = OrderStatus.Pending,
            CreatedAt = _clock.UtcNow,
            ClientOrderId = clientOrderId
        };
        return Task.FromResult(order);
    }

    /// <summary>
    /// Processes a bar: fills pending orders and checks SL/TP on positions.
    /// Must be called by the backtest orchestrator on each bar.
    /// </summary>
    public void ProcessBar(Bar bar)
    {
        // 1. Fill pending orders at this bar's OPEN (T+1 execution)
        FillPendingOrders(bar);

        // 2. Check SL/TP on existing positions
        ProcessStopsAndTargets(bar);
    }

    private void FillPendingOrders(Bar bar)
    {
        foreach (var pending in _pendingOrders.ToList())
        {
            if (pending.SymbolId != bar.SymbolId) continue;

            // Calculate fill price with spread
            double fillPrice = pending.Side == OrderSide.Buy
                ? bar.Open + _settings.SpreadPoints  // Buy at Ask
                : bar.Open;                           // Sell at Bid

            // Create position
            var position = new SimulatedPosition
            {
                PositionId = Guid.NewGuid().ToString("N")[..8],
                SymbolId = pending.SymbolId,
                Side = pending.Side,
                Volume = pending.Volume,
                EntryPrice = fillPrice,
                StopLoss = pending.StopLoss,
                TakeProfit = pending.TakeProfit,
                EntryBarTimestamp = bar.Timestamp,
                OpenedAt = _clock.UtcNow,
                StrategyName = pending.ClientOrderId?.Split('-').FirstOrDefault()
            };
            _openPositions.Add(position);

            // Record trade/fill
            var trade = new Trade
            {
                TradeId = Guid.NewGuid().ToString("N")[..8],
                OrderId = pending.OrderId,
                SymbolId = pending.SymbolId,
                Side = pending.Side,
                Volume = pending.Volume,
                Price = fillPrice,
                Commission = new Money(_settings.CommissionPerLot * (decimal)pending.Volume.Value),
                ExecutedAt = _clock.UtcNow
            };
            _trades.Add(trade);

            _pendingOrders.Remove(pending);
        }
    }

    private void ProcessStopsAndTargets(Bar bar)
    {
        foreach (var position in _openPositions.ToList())
        {
            if (position.SymbolId != bar.SymbolId) continue;

            // CRITICAL: Cannot close on entry bar (T+1 rule)
            if (bar.Timestamp <= position.EntryBarTimestamp) continue;

            if (position.Side == OrderSide.Buy)
            {
                ProcessLongPositionExits(bar, position);
            }
            else
            {
                ProcessShortPositionExits(bar, position);
            }
        }
    }

    private void ProcessLongPositionExits(Bar bar, SimulatedPosition position)
    {
        // Long position: exits at bid prices (High/Low minus spread)
        double bidLow = bar.Low - _settings.SpreadPoints;
        double bidHigh = bar.High - _settings.SpreadPoints;

        // Stop loss triggers on bid low
        if (position.StopLoss.HasValue && bidLow <= position.StopLoss.Value)
        {
            double exitPrice = position.StopLoss.Value - _settings.SlippagePoints;
            ClosePosition(position, exitPrice, "SL");
            return;
        }

        // Take profit triggers on bid high
        if (position.TakeProfit.HasValue && bidHigh >= position.TakeProfit.Value)
        {
            ClosePosition(position, position.TakeProfit.Value, "TP");
        }
    }

    private void ProcessShortPositionExits(Bar bar, SimulatedPosition position)
    {
        // Short position: exits at ask prices (High/Low plus spread)
        double askLow = bar.Low + _settings.SpreadPoints;
        double askHigh = bar.High + _settings.SpreadPoints;

        // Stop loss triggers on ask high
        if (position.StopLoss.HasValue && askHigh >= position.StopLoss.Value)
        {
            double exitPrice = position.StopLoss.Value + _settings.SlippagePoints;
            ClosePosition(position, exitPrice, "SL");
            return;
        }

        // Take profit triggers on ask low
        if (position.TakeProfit.HasValue && askLow <= position.TakeProfit.Value)
        {
            ClosePosition(position, position.TakeProfit.Value, "TP");
        }
    }

    private void ClosePosition(SimulatedPosition position, double exitPrice, string reason)
    {
        _openPositions.Remove(position);
        _closedPositions.Add(position);

        // Record closing trade (opposite side)
        var closingSide = position.Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
        var trade = new Trade
        {
            TradeId = Guid.NewGuid().ToString("N")[..8],
            OrderId = position.PositionId, // Link to position
            SymbolId = position.SymbolId,
            Side = closingSide,
            Volume = position.Volume,
            Price = exitPrice,
            Commission = new Money(_settings.CommissionPerLot * (decimal)position.Volume.Value),
            ExecutedAt = _clock.UtcNow
        };
        _trades.Add(trade);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Position>> GetPositionsAsync(CancellationToken ct = default)
    {
        var positions = _openPositions.Select(p => new Position
        {
            PositionId = p.PositionId,
            SymbolId = p.SymbolId,
            Side = p.Side,
            Volume = p.Volume,
            EntryPrice = p.EntryPrice,
            StopLoss = p.StopLoss,
            TakeProfit = p.TakeProfit,
            OpenedAt = p.OpenedAt,
            StrategyName = p.StrategyName
        }).ToList();
        return Task.FromResult<IReadOnlyList<Position>>(positions);
    }

    /// <inheritdoc />
    public Task ClosePositionAsync(string positionId, CancellationToken ct = default)
    {
        var position = _openPositions.FirstOrDefault(p => p.PositionId == positionId);
        if (position != null)
        {
            _openPositions.Remove(position);
            _closedPositions.Add(position);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Order> ModifyOrderAsync(string orderId, double? stopLoss = null, double? takeProfit = null, CancellationToken ct = default)
    {
        var pending = _pendingOrders.FirstOrDefault(o => o.OrderId == orderId);
        if (pending != null)
        {
            var index = _pendingOrders.IndexOf(pending);
            _pendingOrders[index] = pending with
            {
                StopLoss = stopLoss ?? pending.StopLoss,
                TakeProfit = takeProfit ?? pending.TakeProfit
            };
            pending = _pendingOrders[index];
        }

        return Task.FromResult(new Order
        {
            OrderId = orderId,
            SymbolId = pending?.SymbolId ?? default,
            Side = pending?.Side ?? OrderSide.Buy,
            Type = pending?.Type ?? OrderType.Market,
            Volume = pending?.Volume ?? 0,
            StopLoss = stopLoss ?? pending?.StopLoss,
            TakeProfit = takeProfit ?? pending?.TakeProfit,
            Status = OrderStatus.Pending,
            CreatedAt = pending?.CreatedAt ?? _clock.UtcNow
        });
    }

    /// <inheritdoc />
    public Task CancelOrderAsync(string orderId, CancellationToken ct = default)
    {
        var pending = _pendingOrders.FirstOrDefault(o => o.OrderId == orderId);
        if (pending != null)
        {
            _pendingOrders.Remove(pending);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all state. Useful for test cleanup or reset between backtests.
    /// </summary>
    public void Clear()
    {
        _pendingOrders.Clear();
        _openPositions.Clear();
        _closedPositions.Clear();
        _trades.Clear();
    }
}
