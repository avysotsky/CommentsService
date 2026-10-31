using CommentsService.Infrastructure.Caching;
using CommentsService.Application.Events;
using Xunit;

namespace CommentsService.Data.Tests;

public sealed class CommentReadCacheTests
{
    private static readonly CommentCreated Changed = new(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), DateTimeOffset.UtcNow);
    private sealed class Clock : TimeProvider
    {
        public long Now;
        public override long TimestampFrequency => 1;
        public override long GetTimestamp() => Now;
    }

    [Fact]
    public async Task HitsExpireAndEventsInvalidateImmediately()
    {
        var clock = new Clock(); var cache = new CommentReadCache(new(), clock); var reads = 0;
        Task<int> Read() => cache.ReadAsync("page", () => Task.FromResult(++reads), _ => 1, default);
        Assert.Equal(1, await Read()); Assert.Equal(1, await Read());
        clock.Now = 15;
        Assert.Equal(2, await Read());
        cache.Handle(Changed);
        Assert.Equal(3, await Read());
        Assert.Equal(1, cache.Statistics.Hits);
    }

    [Fact]
    public async Task InvalidationDuringFillPreventsStaleReinsertion()
    {
        var cache = new CommentReadCache(new(), TimeProvider.System);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = cache.ReadAsync("page", () => { started.SetResult(); return finish.Task; }, _ => 1, default);
        await started.Task; cache.Handle(Changed); finish.SetResult(1);
        Assert.Equal(1, await pending);
        Assert.Equal(0, cache.Statistics.Count);
        Assert.Equal(2, await cache.ReadAsync("page", () => Task.FromResult(2), _ => 1, default));
    }

    [Fact]
    public async Task BoundsEvictOldestAndOversizedResultsAreNotCached()
    {
        var cache = new CommentReadCache(new() { Capacity = 2, MaximumBytes = 1024 }, TimeProvider.System);
        Task<int> Read(string key, int value, int size = 400) => cache.ReadAsync(key, () => Task.FromResult(value), _ => size, default);
        await Read("a", 1); await Read("b", 2); await Read("c", 3);
        Assert.Equal(2, cache.Statistics.Count); Assert.Equal(800, cache.Statistics.Bytes);
        Assert.Equal(4, await Read("a", 4));
        await Read("large", 5, 2048);
        Assert.Equal(6, await Read("large", 6, 2048));
    }

    [Fact]
    public async Task FailedCancelledAndDisabledReadsDoNotPopulate()
    {
        var cache = new CommentReadCache(new(), TimeProvider.System);
        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.ReadAsync<int>("x", () => throw new InvalidOperationException(), _ => 1, default));
        using var cts = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.ReadAsync("x", () => { cts.Cancel(); return Task.FromResult(1); }, _ => 1, cts.Token));
        Assert.Equal(0, cache.Statistics.Count);
        var disabled = new CommentReadCache(new() { Enabled = false }, TimeProvider.System);
        await disabled.ReadAsync("x", () => Task.FromResult(1), _ => 1, default);
        Assert.Equal(0, disabled.Statistics.Count);
    }

    [Fact]
    public async Task KeysIncludeAllPageAndSortParameters()
    {
        var cache = new CommentReadCache(new(), TimeProvider.System);
        var keys = new[] {
            new CommentsService.Application.Comments.GetRootPage.RootPageQuery(1, 0, 0, 25),
            new CommentsService.Application.Comments.GetRootPage.RootPageQuery(2, 0, 0, 25),
            new CommentsService.Application.Comments.GetRootPage.RootPageQuery(1, (CommentsService.Application.Comments.GetRootPage.CommentSort)1, 0, 25),
            new CommentsService.Application.Comments.GetRootPage.RootPageQuery(1, 0, (CommentsService.Application.Comments.GetRootPage.SortDirection)1, 25),
            new CommentsService.Application.Comments.GetRootPage.RootPageQuery(1, 0, 0, 10) };
        for (var i = 0; i < keys.Length; i++)
            Assert.Equal(i, await cache.ReadAsync(keys[i], () => Task.FromResult(i), _ => 1, default));
        Assert.Equal(5, cache.Statistics.Count);
    }

    [Theory]
    [InlineData(0, 128, 1024)] [InlineData(301, 128, 1024)]
    [InlineData(15, 0, 1024)] [InlineData(15, 4097, 1024)] [InlineData(15, 128, 1023)]
    public void InvalidPoliciesFailStartup(int ttl, int capacity, long bytes) =>
        Assert.Throws<InvalidOperationException>(() => new CacheSettings { LifetimeSeconds = ttl, Capacity = capacity, MaximumBytes = bytes }.Validate());
}
