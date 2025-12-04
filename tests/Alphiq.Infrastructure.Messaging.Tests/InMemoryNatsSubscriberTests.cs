using FluentAssertions;
using Xunit;
using Alphiq.Contracts;
using Alphiq.Domain.ValueObjects;
using Alphiq.Infrastructure.Messaging.Testing;

namespace Alphiq.Infrastructure.Messaging.Tests;

public class InMemoryNatsSubscriberTests
{
    [Fact]
    public async Task SubscribeAsync_YieldsEnqueuedMessages()
    {
        var subscriber = new InMemoryNatsSubscriber<BacktestJob>();
        var job = CreateTestJob();

        subscriber.Enqueue(job);
        subscriber.Complete();

        var received = new List<BacktestJob>();
        await foreach (var msg in subscriber.SubscribeAsync())
        {
            received.Add(msg.Data);
            await msg.AckAsync(default);
        }

        received.Should().HaveCount(1);
        received[0].JobId.Should().Be(job.JobId);
    }

    [Fact]
    public async Task SubscribeAsync_TracksAcknowledgments()
    {
        var subscriber = new InMemoryNatsSubscriber<BacktestJob>();
        var job = CreateTestJob();

        subscriber.Enqueue(job);
        subscriber.Complete();

        await foreach (var msg in subscriber.SubscribeAsync())
        {
            await msg.AckAsync(default);
        }

        subscriber.AcknowledgedMessages.Should().HaveCount(1);
        subscriber.AcknowledgedMessages[0].JobId.Should().Be(job.JobId);
    }

    [Fact]
    public async Task SubscribeAsync_TracksNegativeAcknowledgments()
    {
        var subscriber = new InMemoryNatsSubscriber<BacktestJob>();
        var job = CreateTestJob();

        subscriber.Enqueue(job);
        subscriber.Complete();

        await foreach (var msg in subscriber.SubscribeAsync())
        {
            await msg.NakAsync(default);
        }

        subscriber.NegativeAcknowledgedMessages.Should().HaveCount(1);
        subscriber.NegativeAcknowledgedMessages[0].JobId.Should().Be(job.JobId);
    }

    [Fact]
    public async Task SubscribeAsync_YieldsMultipleMessagesInOrder()
    {
        var subscriber = new InMemoryNatsSubscriber<BacktestJob>();
        var jobs = Enumerable.Range(1, 5)
            .Select(i => CreateTestJob($"job-{i}"))
            .ToList();

        foreach (var job in jobs)
        {
            subscriber.Enqueue(job);
        }
        subscriber.Complete();

        var received = new List<string>();
        await foreach (var msg in subscriber.SubscribeAsync())
        {
            received.Add(msg.Data.JobId);
            await msg.AckAsync(default);
        }

        received.Should().BeEquivalentTo(jobs.Select(j => j.JobId), options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task SubscribeAsync_StopsOnCancellation()
    {
        var subscriber = new InMemoryNatsSubscriber<BacktestJob>();
        var cts = new CancellationTokenSource();

        // Enqueue only 2 messages, then test that cancellation stops waiting for more
        subscriber.Enqueue(CreateTestJob());
        subscriber.Enqueue(CreateTestJob());

        var received = new List<BacktestJob>();

        // Start consuming in a background task
        var consumeTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var msg in subscriber.SubscribeAsync(cts.Token))
                {
                    received.Add(msg.Data);
                    await msg.AckAsync(default);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        });

        // Wait a bit for messages to be consumed
        await Task.Delay(50);

        // Cancel while waiting for more messages
        cts.Cancel();

        // Wait for the consume task to complete
        await consumeTask;

        // Should have received the 2 messages that were enqueued
        received.Should().HaveCount(2);
    }

    [Fact]
    public async Task ClearAcknowledgments_ResetsTracking()
    {
        var subscriber = new InMemoryNatsSubscriber<BacktestJob>();
        subscriber.Enqueue(CreateTestJob());
        subscriber.Complete();

        await foreach (var msg in subscriber.SubscribeAsync())
        {
            await msg.AckAsync(default);
        }

        subscriber.AcknowledgedMessages.Should().HaveCount(1);

        subscriber.ClearAcknowledgments();

        subscriber.AcknowledgedMessages.Should().BeEmpty();
        subscriber.NegativeAcknowledgedMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task SubscribeAsync_EmptyChannel_CompletesImmediately()
    {
        var subscriber = new InMemoryNatsSubscriber<BacktestJob>();
        subscriber.Complete();

        var received = new List<BacktestJob>();
        await foreach (var msg in subscriber.SubscribeAsync())
        {
            received.Add(msg.Data);
        }

        received.Should().BeEmpty();
    }

    private static BacktestJob CreateTestJob(string? jobId = null) => new()
    {
        JobId = jobId ?? Guid.NewGuid().ToString(),
        StrategyName = "TestStrategy",
        StrategyVersion = 1,
        Symbols = [new SymbolId(1)],
        StartDate = DateTimeOffset.UtcNow.AddDays(-30),
        EndDate = DateTimeOffset.UtcNow,
        Parameters = new Dictionary<string, object>(),
        RequestedAt = DateTimeOffset.UtcNow
    };
}
