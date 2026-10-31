using CommentsService.Api.Notifications;
using CommentsService.Application.Events;
using CommentsService.Application.Notifications;
using CommentsService.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CommentsService.Api.Tests;

public sealed class NotificationQueueTests
{
    private static readonly CommentCreated Changed = new(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public void QueueBoundsPendingHintsAndPreservesARefreshUnderOverflow()
    {
        var queue = new CommentNotificationQueue(new() { Capacity = 2 });
        for (var i = 0; i < 100; i++) queue.Handle(Changed);
        Assert.Equal(98, queue.Coalesced);
        Assert.True(queue.Reader.TryRead(out _)); Assert.True(queue.Reader.TryRead(out _));
        Assert.False(queue.Reader.TryRead(out _));
    }

    [Theory]
    [InlineData(2)] [InlineData(10)]
    public async Task WorkerRetriesAndKeepsProcessingAfterFailure(int failures)
    {
        var settings = new NotificationSettings { MaximumAttempts = 3, RetryDelayMilliseconds = 1 };
        var queue = new CommentNotificationQueue(settings);
        var delivery = new Delivery(failures);
        using var worker = new CommentNotificationWorker(queue, settings, delivery, NullLogger<CommentNotificationWorker>.Instance);
        queue.Handle(Changed);
        if (failures > 3) queue.Handle(Changed);
        queue.Complete();
        await worker.StartAsync(default);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(failures == 2 ? 3 : 6, delivery.Calls);
        await worker.StopAsync(default);
    }

    [Fact]
    public async Task IdleWorkerStopsPromptlyAndCompletesQueue()
    {
        var settings = new NotificationSettings(); var queue = new CommentNotificationQueue(settings);
        using var worker = new CommentNotificationWorker(queue, settings, new Delivery(0), NullLogger<CommentNotificationWorker>.Instance);
        await worker.StartAsync(default);
        await worker.StopAsync(default).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(queue.Reader.Completion.IsCompleted);
    }

    [Theory]
    [InlineData(0,3,200)] [InlineData(4097,3,200)] [InlineData(64,0,200)] [InlineData(64,11,200)] [InlineData(64,3,0)]
    public void InvalidPoliciesAreRejected(int capacity, int attempts, int delay) =>
        Assert.Throws<InvalidOperationException>(() => new NotificationSettings { Capacity = capacity, MaximumAttempts = attempts, RetryDelayMilliseconds = delay }.Validate());

    private sealed class Delivery(int failures) : ICommentChangeDelivery
    {
        public int Calls;
        public Task DeliverAsync(CancellationToken cancellationToken)
        {
            Calls++;
            if (Calls <= failures) throw new InvalidOperationException("private detail");
            return Task.CompletedTask;
        }
    }
}
