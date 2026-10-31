using System.Net;
using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Application.Comments;
using CommentsService.Application.Comments.CreateComment;
using CommentsService.Application.Comments.GetReplies;
using CommentsService.Application.Comments.Markup;
using CommentsService.Domain.Entities;
using Xunit;
namespace CommentsService.Application.Tests;

public sealed class CommentThreadTests
{
    private static readonly ClientMetadata Client = new(IPAddress.Loopback, "Thread test");
    private static CreateCommentInput Input(Guid? parent = null) => new("Reader", "r@example.test", null, "<i>reply</i>", parent);
    [Fact]
    public async Task MissingParentDoesNotStageAuthorOrSave()
    {
        var store = new Store();
        var result = await new CreateComment(store, store, new XhtmlCommentMarkup(), new AcceptCaptcha(), new TestImageProcessor(), new TestTextFileProcessor()).ExecuteAsync(Input(Guid.NewGuid()), Client);
        Assert.False(result.IsValid); Assert.Contains("parentId", result.Errors.Keys);
        Assert.Empty(store.Rows); Assert.Equal(0, store.Saves);
    }
    [Fact]
    public async Task AnyExistingCommentCanReceiveAReplyAndRootIsDerived()
    {
        var store = new Store(); var create = new CreateComment(store, store, new XhtmlCommentMarkup(), new AcceptCaptcha(), new TestImageProcessor(), new TestTextFileProcessor());
        var root = (await create.ExecuteAsync(Input(), Client)).Value!;
        var parent = root;
        for (var i = 0; i < 100; i++)
        {
            var child = (await create.ExecuteAsync(Input(parent.Id), Client)).Value!;
            Assert.Equal(parent.Id, child.ParentId); Assert.Equal(root.Id, child.RootId); parent = child;
        }
        Assert.Equal(101, store.Saves);
    }
    [Theory]
    [InlineData(0)] [InlineData(101)]
    public async Task InvalidReplyLimitDoesNotReadStorage(int limit)
    {
        var store = new Store(); var result = await new GetReplies(store).ExecuteAsync(Guid.NewGuid(), limit);
        Assert.False(result.IsValid); Assert.Equal(0, store.Reads);
    }
    [Fact]
    public async Task MissingParentAndForeignCursorAreRejected()
    {
        var store = new Store(); var get = new GetReplies(store);
        Assert.Contains("parentId", (await get.ExecuteAsync(Guid.NewGuid())).Errors.Keys);
        var create = new CreateComment(store, store, new XhtmlCommentMarkup(), new AcceptCaptcha(), new TestImageProcessor(), new TestTextFileProcessor());
        var root = (await create.ExecuteAsync(Input(), Client)).Value!;
        Assert.Contains("after", (await get.ExecuteAsync(root.Id, after: root.Id)).Errors.Keys);
        Assert.Contains("after", (await get.ExecuteAsync(root.Id, after: Guid.NewGuid())).Errors.Keys);
    }
    [Fact]
    public async Task ReplyPageUsesAnExtraRowAndPassesCursorAndCancellation()
    {
        var store = new Store(); var create = new CreateComment(store, store, new XhtmlCommentMarkup(), new AcceptCaptcha(), new TestImageProcessor(), new TestTextFileProcessor());
        var root = (await create.ExecuteAsync(Input(), Client)).Value!;
        for (var i = 0; i < 3; i++) await create.ExecuteAsync(Input(root.Id), Client);
        var get = new GetReplies(store);
        var first = (await get.ExecuteAsync(root.Id, 2)).Value!;
        Assert.Equal(2, first.Items.Count); Assert.Equal(first.Items[^1].Id, first.NextAfter);
        using var cancel = new CancellationTokenSource();
        var second = (await get.ExecuteAsync(root.Id, 2, first.NextAfter, cancel.Token)).Value!;
        Assert.Single(second.Items); Assert.Null(second.NextAfter); Assert.Equal(3, store.Limit);
        Assert.Equal(first.NextAfter, store.Cursor!.Id); Assert.Equal(cancel.Token, store.Token);
        Assert.Empty(first.Items.Select(x => x.Id).Intersect(second.Items.Select(x => x.Id)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => get.ExecuteAsync(root.Id, cancellationToken: new(true)));
    }
    private sealed class Store : ICommentRepository, IUnitOfWork, ICommentQueries
    {
        public List<Comment> Rows { get; } = [];
        public int Saves, Reads, Limit; public ReplyCursor? Cursor; public CancellationToken Token;
        public void Add(Comment comment) => Rows.Add(comment);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { Saves++; return Task.FromResult(2); }
        Task<Comment?> ICommentRepository.FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Rows.SingleOrDefault(x => x.Id == id));
        public Task<CommentDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        { Reads++; return Task.FromResult(Rows.SingleOrDefault(x => x.Id == id) is { } c ? CommentMapping.From(c) : null); }
        public Task<IReadOnlyList<CommentDto>> GetLatestAsync(int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CommentDto>>(Rows.Where(x => x.ParentId == null).Select(CommentMapping.From).Take(limit).ToArray());
        public Task<CommentsService.Application.Comments.GetRootPage.RootCommentsPage> GetRootPageAsync(
            CommentsService.Application.Comments.GetRootPage.RootPageQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CommentDto>> GetRepliesAsync(Guid parentId, int limit, ReplyCursor? after, CancellationToken cancellationToken = default)
        {
            Reads++; Limit = limit; Cursor = after; Token = cancellationToken;
            var ordered = Rows.Where(x => x.ParentId == parentId).Reverse().Select(CommentMapping.From);
            if (after is not null) ordered = ordered.SkipWhile(x => x.Id != after.Id).Skip(1);
            return Task.FromResult<IReadOnlyList<CommentDto>>(ordered.Take(limit).ToArray());
        }
    }
}
