using FluentAssertions;
using Xunit;
using Alphiq.Domain.Entities;
using Alphiq.Domain.Enums;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.Infrastructure.Broker.Simulated.Tests;

public class SimulatedOrderExecutionTests
{
    private readonly BacktestClock _clock;
    private readonly BacktestSettings _settings;
    private readonly SimulatedOrderExecution _sut;
    private static readonly SymbolId TestSymbolId = new(1);

    public SimulatedOrderExecutionTests()
    {
        _clock = new BacktestClock(DateTimeOffset.Parse("2024-01-15T10:00:00Z"));
        _settings = new BacktestSettings { SpreadPoints = 0.0004, SlippagePoints = 0.0001 };
        _sut = new SimulatedOrderExecution(_clock, _settings);
    }

    #region PlaceOrderAsync Tests

    [Fact]
    public async Task PlaceOrderAsync_ReturnsOrderInPendingState()
    {
        var order = await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, null, null, null, default);

        order.Status.Should().Be(OrderStatus.Pending);
        order.SymbolId.Should().Be(TestSymbolId);
        order.Side.Should().Be(OrderSide.Buy);
        order.Volume.Value.Should().Be(0.01);
    }

    [Fact]
    public async Task PlaceOrderAsync_DoesNotFillImmediately()
    {
        await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, null, null, null, default);

        _sut.OpenPositions.Should().BeEmpty();
        _sut.PendingOrders.Should().HaveCount(1);
    }

    [Fact]
    public async Task PlaceOrderAsync_StoresSLAndTP()
    {
        var order = await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, 1.0950, 1.1100, "test-order", default);

        order.StopLoss.Should().Be(1.0950);
        order.TakeProfit.Should().Be(1.1100);
        _sut.PendingOrders[0].StopLoss.Should().Be(1.0950);
        _sut.PendingOrders[0].TakeProfit.Should().Be(1.1100);
    }

    #endregion

    #region ProcessBar - Order Fill Tests

    [Fact]
    public async Task ProcessBar_FillsLongOrderAtOpenPlusSpread()
    {
        await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, null, null, null, default);

        var bar = CreateBar(TestSymbolId, 1705315500, open: 1.1000);
        _clock.AdvanceToBarClose(bar);
        _sut.ProcessBar(bar);

        _sut.OpenPositions.Should().HaveCount(1);
        _sut.OpenPositions[0].EntryPrice.Should().Be(1.1000 + 0.0004); // Open + Spread
        _sut.PendingOrders.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessBar_FillsShortOrderAtOpenPrice()
    {
        await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Sell, OrderType.Market, 0.01,
            null, null, null, null, default);

        var bar = CreateBar(TestSymbolId, 1705315500, open: 1.1000);
        _clock.AdvanceToBarClose(bar);
        _sut.ProcessBar(bar);

        _sut.OpenPositions.Should().HaveCount(1);
        _sut.OpenPositions[0].EntryPrice.Should().Be(1.1000); // Bid price
    }

    [Fact]
    public async Task ProcessBar_CreatesTradeRecord()
    {
        await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, null, null, null, default);

        var bar = CreateBar(TestSymbolId, 1705315500, open: 1.1000);
        _clock.AdvanceToBarClose(bar);
        _sut.ProcessBar(bar);

        _sut.Trades.Should().HaveCount(1);
        _sut.Trades[0].Price.Should().Be(1.1000 + 0.0004);
        _sut.Trades[0].Commission.Amount.Should().Be(0.03m); // 3.0 * 0.01
    }

    [Fact]
    public async Task ProcessBar_DoesNotFillOrderForDifferentSymbol()
    {
        await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, null, null, null, default);

        var bar = CreateBar(new SymbolId(999), 1705315500, open: 1.1000);
        _clock.AdvanceToBarClose(bar);
        _sut.ProcessBar(bar);

        _sut.OpenPositions.Should().BeEmpty();
        _sut.PendingOrders.Should().HaveCount(1);
    }

    #endregion

    #region ProcessBar - Stop Loss Tests

    [Fact]
    public async Task ProcessBar_StopLossNotTriggeredOnEntryBar()
    {
        await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, 1.0950, null, null, default);

        // Bar 2: fills order, but low goes through SL - should NOT trigger
        var bar2 = CreateBar(TestSymbolId, 1705315500, open: 1.1000, low: 1.0900);
        _clock.AdvanceToBarClose(bar2);
        _sut.ProcessBar(bar2);

        _sut.OpenPositions.Should().HaveCount(1);
        _sut.ClosedPositions.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessBar_StopLossTriggersOnSubsequentBar()
    {
        await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, 1.0950, null, null, default);

        // Bar 2: fills order
        var bar2 = CreateBar(TestSymbolId, 1705315500, open: 1.1000, low: 1.0960);
        _clock.AdvanceToBarClose(bar2);
        _sut.ProcessBar(bar2);

        _sut.OpenPositions.Should().HaveCount(1);

        // Bar 3: SL triggers
        var bar3 = CreateBar(TestSymbolId, 1705315800, open: 1.0980, low: 1.0940);
        _clock.AdvanceToBarClose(bar3);
        _sut.ProcessBar(bar3);

        _sut.OpenPositions.Should().BeEmpty();
        _sut.ClosedPositions.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessBar_LongSLExitsAtSLPriceMinusSlippage()
    {
        await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, 1.0950, null, null, default);

        // Fill the order
        var bar2 = CreateBar(TestSymbolId, 1705315500, open: 1.1000);
        _clock.AdvanceToBarClose(bar2);
        _sut.ProcessBar(bar2);

        // Trigger SL
        var bar3 = CreateBar(TestSymbolId, 1705315800, open: 1.0980, low: 1.0940);
        _clock.AdvanceToBarClose(bar3);
        _sut.ProcessBar(bar3);

        // SL price - slippage
        var expectedExitPrice = 1.0950 - 0.0001;
        _sut.Trades.Last().Price.Should().BeApproximately(expectedExitPrice, 0.00001);
    }

    [Fact]
    public async Task ProcessBar_ShortSLExitsAtSLPricePlusSlippage()
    {
        await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Sell, OrderType.Market, 0.01,
            null, 1.1050, null, null, default);

        // Fill the order
        var bar2 = CreateBar(TestSymbolId, 1705315500, open: 1.1000);
        _clock.AdvanceToBarClose(bar2);
        _sut.ProcessBar(bar2);

        // Trigger SL on ask high
        var bar3 = CreateBar(TestSymbolId, 1705315800, open: 1.1020, high: 1.1060);
        _clock.AdvanceToBarClose(bar3);
        _sut.ProcessBar(bar3);

        // SL price + slippage
        var expectedExitPrice = 1.1050 + 0.0001;
        _sut.Trades.Last().Price.Should().BeApproximately(expectedExitPrice, 0.00001);
    }

    #endregion

    #region ProcessBar - Take Profit Tests

    [Fact]
    public async Task ProcessBar_LongTakeProfitTriggersOnBidHigh()
    {
        await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, null, 1.1100, null, default);

        // Fill the order
        var bar2 = CreateBar(TestSymbolId, 1705315500, open: 1.1000);
        _clock.AdvanceToBarClose(bar2);
        _sut.ProcessBar(bar2);

        // TP triggers on bid high >= TP
        // Bid high = bar.High - spread = 1.1150 - 0.0004 = 1.1146
        var bar3 = CreateBar(TestSymbolId, 1705315800, open: 1.1050, high: 1.1150);
        _clock.AdvanceToBarClose(bar3);
        _sut.ProcessBar(bar3);

        _sut.OpenPositions.Should().BeEmpty();
        _sut.ClosedPositions.Should().HaveCount(1);
        _sut.Trades.Last().Price.Should().Be(1.1100); // Exits at TP price
    }

    [Fact]
    public async Task ProcessBar_ShortTakeProfitTriggersOnAskLow()
    {
        await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Sell, OrderType.Market, 0.01,
            null, null, 1.0900, null, default);

        // Fill the order
        var bar2 = CreateBar(TestSymbolId, 1705315500, open: 1.1000);
        _clock.AdvanceToBarClose(bar2);
        _sut.ProcessBar(bar2);

        // TP triggers on ask low <= TP
        // Ask low = bar.Low + spread = 1.0850 + 0.0004 = 1.0854
        var bar3 = CreateBar(TestSymbolId, 1705315800, open: 1.0950, low: 1.0850);
        _clock.AdvanceToBarClose(bar3);
        _sut.ProcessBar(bar3);

        _sut.OpenPositions.Should().BeEmpty();
        _sut.ClosedPositions.Should().HaveCount(1);
        _sut.Trades.Last().Price.Should().Be(1.0900); // Exits at TP price
    }

    #endregion

    #region Other Interface Methods

    [Fact]
    public async Task GetPositionsAsync_ReturnsMappedPositions()
    {
        await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, 1.0950, 1.1100, null, default);

        var bar = CreateBar(TestSymbolId, 1705315500, open: 1.1000);
        _clock.AdvanceToBarClose(bar);
        _sut.ProcessBar(bar);

        var positions = await _sut.GetPositionsAsync(default);

        positions.Should().HaveCount(1);
        positions[0].StopLoss.Should().Be(1.0950);
        positions[0].TakeProfit.Should().Be(1.1100);
    }

    [Fact]
    public async Task CancelOrderAsync_RemovesPendingOrder()
    {
        var order = await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, null, null, null, default);

        await _sut.CancelOrderAsync(order.OrderId, default);

        _sut.PendingOrders.Should().BeEmpty();
    }

    [Fact]
    public async Task ModifyOrderAsync_UpdatesSLAndTP()
    {
        var order = await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, 1.0950, 1.1100, null, default);

        await _sut.ModifyOrderAsync(order.OrderId, 1.0900, 1.1200, default);

        _sut.PendingOrders[0].StopLoss.Should().Be(1.0900);
        _sut.PendingOrders[0].TakeProfit.Should().Be(1.1200);
    }

    [Fact]
    public async Task ClosePositionAsync_ClosesPosition()
    {
        await _sut.PlaceOrderAsync(
            TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, null, null, null, default);

        var bar = CreateBar(TestSymbolId, 1705315500, open: 1.1000);
        _clock.AdvanceToBarClose(bar);
        _sut.ProcessBar(bar);

        var positionId = _sut.OpenPositions[0].PositionId;
        await _sut.ClosePositionAsync(positionId, default);

        _sut.OpenPositions.Should().BeEmpty();
        _sut.ClosedPositions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Clear_ResetsAllState()
    {
        await _sut.PlaceOrderAsync(TestSymbolId, OrderSide.Buy, OrderType.Market, 0.01,
            null, null, null, null, default);

        var bar = CreateBar(TestSymbolId, 1705315500, open: 1.1000);
        _clock.AdvanceToBarClose(bar);
        _sut.ProcessBar(bar);

        _sut.Clear();

        _sut.PendingOrders.Should().BeEmpty();
        _sut.OpenPositions.Should().BeEmpty();
        _sut.ClosedPositions.Should().BeEmpty();
        _sut.Trades.Should().BeEmpty();
    }

    #endregion

    private static Bar CreateBar(
        SymbolId symbolId,
        long timestamp,
        double open = 1.1,
        double high = 1.11,
        double low = 1.09,
        double close = 1.105)
    {
        return new Bar
        {
            SymbolId = symbolId,
            Timeframe = Timeframe.M5,
            Timestamp = timestamp,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = 1000
        };
    }
}
