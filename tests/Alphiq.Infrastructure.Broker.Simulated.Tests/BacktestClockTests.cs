using FluentAssertions;
using Xunit;
using Alphiq.Brokers.Abstractions;
using Alphiq.Domain.Entities;
using Alphiq.Domain.ValueObjects;

namespace Alphiq.Infrastructure.Broker.Simulated.Tests;

public class BacktestClockTests
{
    [Fact]
    public void Constructor_Default_SetsTimeToMinValue()
    {
        var clock = new BacktestClock();

        clock.UtcNow.Should().Be(DateTimeOffset.MinValue);
    }

    [Fact]
    public void Constructor_WithStartTime_SetsTime()
    {
        var startTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var clock = new BacktestClock(startTime);

        clock.UtcNow.Should().Be(startTime);
    }

    [Fact]
    public void AdvanceTo_MovesTimeForward()
    {
        var startTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var clock = new BacktestClock(startTime);
        var newTime = startTime.AddHours(1);

        clock.AdvanceTo(newTime);

        clock.UtcNow.Should().Be(newTime);
    }

    [Fact]
    public void AdvanceTo_SameTime_DoesNotThrow()
    {
        var startTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var clock = new BacktestClock(startTime);

        var act = () => clock.AdvanceTo(startTime);

        act.Should().NotThrow();
        clock.UtcNow.Should().Be(startTime);
    }

    [Fact]
    public void AdvanceTo_BackwardsTime_ThrowsInvalidOperationException()
    {
        var startTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var clock = new BacktestClock(startTime);
        var pastTime = startTime.AddHours(-1);

        var act = () => clock.AdvanceTo(pastTime);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*backwards*");
    }

    [Fact]
    public void AdvanceToBarClose_AdvancesToBarDateTime()
    {
        var clock = new BacktestClock();
        var bar = CreateBar(1705315200); // 2024-01-15T10:00:00Z

        clock.AdvanceToBarClose(bar);

        clock.UtcNow.Should().Be(bar.DateTime);
    }

    [Fact]
    public void UnixTimeSeconds_ReturnsCorrectValue()
    {
        var timestamp = 1705315200L; // 2024-01-15T10:00:00Z
        var clock = new BacktestClock(DateTimeOffset.FromUnixTimeSeconds(timestamp));

        // UnixTimeSeconds is a default interface member, access via interface
        ((IClock)clock).UnixTimeSeconds.Should().Be(timestamp);
    }

    [Fact]
    public void Reset_SetsTimeWithoutValidation()
    {
        var startTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var clock = new BacktestClock(startTime);
        var pastTime = startTime.AddHours(-5);

        // Reset should allow going backwards (for test setup only)
        clock.Reset(pastTime);

        clock.UtcNow.Should().Be(pastTime);
    }

    [Fact]
    public void MultipleAdvances_AccumulateCorrectly()
    {
        var startTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var clock = new BacktestClock(startTime);

        clock.AdvanceTo(startTime.AddMinutes(5));
        clock.AdvanceTo(startTime.AddMinutes(10));
        clock.AdvanceTo(startTime.AddMinutes(15));

        clock.UtcNow.Should().Be(startTime.AddMinutes(15));
    }

    private static Bar CreateBar(long timestamp)
    {
        return new Bar
        {
            SymbolId = new SymbolId(1),
            Timeframe = Timeframe.M5,
            Timestamp = timestamp,
            Open = 1.1000,
            High = 1.1050,
            Low = 1.0950,
            Close = 1.1025,
            Volume = 1000
        };
    }
}
