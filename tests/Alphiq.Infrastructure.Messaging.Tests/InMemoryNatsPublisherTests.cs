using FluentAssertions;
using Xunit;
using Alphiq.Contracts;
using Alphiq.Domain.ValueObjects;
using Alphiq.Infrastructure.Messaging.Testing;

namespace Alphiq.Infrastructure.Messaging.Tests;

public class InMemoryNatsPublisherTests
{
    [Fact]
    public async Task PublishAsync_CapturesMessage()
    {
        var publisher = new InMemoryNatsPublisher<BacktestJob>();
        var job = CreateTestJob();

        await publisher.PublishAsync(job);

        publisher.PublishedMessages.Should().HaveCount(1);
        publisher.PublishedMessages[0].JobId.Should().Be(job.JobId);
    }

    [Fact]
    public async Task PublishAsync_CapturesMultipleMessages()
    {
        var publisher = new InMemoryNatsPublisher<BacktestJob>();
        var job1 = CreateTestJob();
        var job2 = CreateTestJob();
        var job3 = CreateTestJob();

        await publisher.PublishAsync(job1);
        await publisher.PublishAsync(job2);
        await publisher.PublishAsync(job3);

        publisher.PublishedMessages.Should().HaveCount(3);
        publisher.PublishCount.Should().Be(3);
    }

    [Fact]
    public async Task PublishAsync_ThrowsOnCancellation()
    {
        var publisher = new InMemoryNatsPublisher<BacktestJob>();
        var job = CreateTestJob();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await publisher.PublishAsync(job, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Clear_RemovesAllMessages()
    {
        var publisher = new InMemoryNatsPublisher<BacktestJob>();
        await publisher.PublishAsync(CreateTestJob());
        await publisher.PublishAsync(CreateTestJob());

        publisher.Clear();

        publisher.PublishedMessages.Should().BeEmpty();
        publisher.PublishCount.Should().Be(0);
    }

    [Fact]
    public async Task PublishAsync_PreservesMessageOrder()
    {
        var publisher = new InMemoryNatsPublisher<BacktestJob>();
        var jobs = Enumerable.Range(1, 5)
            .Select(i => CreateTestJob($"job-{i}"))
            .ToList();

        foreach (var job in jobs)
        {
            await publisher.PublishAsync(job);
        }

        publisher.PublishedMessages.Select(j => j.JobId)
            .Should().BeEquivalentTo(jobs.Select(j => j.JobId), options => options.WithStrictOrdering());
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
