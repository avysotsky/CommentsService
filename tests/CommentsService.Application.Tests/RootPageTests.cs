using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Application.Comments;
using CommentsService.Application.Comments.GetReplies;
using CommentsService.Application.Comments.GetRootPage;
using Xunit;
namespace CommentsService.Application.Tests;

public sealed class RootPageTests
{
    [Theory]
    [InlineData(0, "createdAt", "desc", "page")]
    [InlineData(-1, "createdAt", "desc", "page")]
    [InlineData(85899346, "createdAt", "desc", "page")]
    [InlineData(1, "Text", "asc", "sort")]
    [InlineData(1, "userName; DROP TABLE", "asc", "sort")]
    [InlineData(1, "email", "descending", "direction")]
    public async Task InvalidQueriesNeverReachStorage(int page, string sort, string direction, string field)
    {
        var store = new Queries();
        var result = await new GetRootPage(store).ExecuteAsync(page, sort, direction);
        Assert.False(result.IsValid); Assert.Contains(field, result.Errors.Keys); Assert.Equal(0, store.Calls);
    }
    [Theory]
    [InlineData("userName", CommentSort.UserName)]
    [InlineData("email", CommentSort.Email)]
    [InlineData("createdAt", CommentSort.CreatedAt)]
    public async Task SortDirectionsAndCancellationAreMappedToTypedCoreQueries(string sort, CommentSort expected)
    {
        var store = new Queries(); using var cancel = new CancellationTokenSource();
        foreach (var direction in new[] { "asc", "desc" })
        {
            var result = await new GetRootPage(store).ExecuteAsync(2, sort, direction, cancel.Token);
            Assert.True(result.IsValid); Assert.Equal(new RootPageQuery(2, expected, direction == "asc" ? SortDirection.Asc : SortDirection.Desc), store.Query);
            Assert.Equal(cancel.Token, store.Token); Assert.Equal(25, result.Value!.PageSize);
        }
    }
    [Fact]
    public async Task DefaultIsLifoAndCancelledQueryDoesNotRead()
    {
        var store = new Queries(); var get = new GetRootPage(store);
        Assert.True((await get.ExecuteAsync()).IsValid);
        Assert.Equal(new RootPageQuery(1, CommentSort.CreatedAt, SortDirection.Desc), store.Query);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => get.ExecuteAsync(cancellationToken: new(true)));
        Assert.Equal(1, store.Calls);
    }
    private sealed class Queries : ICommentQueries
    {
        public int Calls; public RootPageQuery? Query; public CancellationToken Token;
        public Task<RootCommentsPage> GetRootPageAsync(RootPageQuery query, CancellationToken cancellationToken = default)
        { Calls++; Query = query; Token = cancellationToken; return Task.FromResult(new RootCommentsPage([], query.Page, 25, 0, 0)); }
        public Task<CommentDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CommentDto>> GetLatestAsync(int limit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CommentDto>> GetRepliesAsync(Guid parentId, int limit, ReplyCursor? after, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
