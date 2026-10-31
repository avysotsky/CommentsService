using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Application.Comments;
using CommentsService.Application.Comments.GetRootPage;
using CommentsService.Application.Comments.GetReplies;

namespace CommentsService.Infrastructure.Caching;

public sealed class CachedCommentQueries(ICommentQueries inner, CommentReadCache cache) : ICommentQueries
{
    private sealed record LatestKey(int Limit);
    private static long Weight(IReadOnlyList<CommentDto> items) => 512 + items.Sum(item =>
        1024L + 2L * (item.Text.Length + item.UserName.Length + item.Email.Length + (item.HomePage?.Length ?? 0) +
            (item.Image?.FileName.Length ?? 0) + (item.TextFile?.FileName.Length ?? 0)));

    public Task<RootCommentsPage> GetRootPageAsync(RootPageQuery query, CancellationToken cancellationToken = default) =>
        cache.ReadAsync(query, async () =>
        {
            var page = await inner.GetRootPageAsync(query, cancellationToken);
            return page with { Items = Array.AsReadOnly(page.Items.ToArray()) };
        }, page => Weight(page.Items), cancellationToken);

    public Task<IReadOnlyList<CommentDto>> GetLatestAsync(int limit, CancellationToken cancellationToken = default) =>
        cache.ReadAsync<IReadOnlyList<CommentDto>>(new LatestKey(limit), async () =>
            Array.AsReadOnly((await inner.GetLatestAsync(limit, cancellationToken)).ToArray()), Weight, cancellationToken);

    // Replies, individual records and attachments remain direct reads.
    public Task<CommentDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        inner.FindByIdAsync(id, cancellationToken);
    public Task<IReadOnlyList<CommentDto>> GetRepliesAsync(Guid parentId, int limit, ReplyCursor? after, CancellationToken cancellationToken = default) =>
        inner.GetRepliesAsync(parentId, limit, after, cancellationToken);
}
