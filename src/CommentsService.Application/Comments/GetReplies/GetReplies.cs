using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Application.Common;

namespace CommentsService.Application.Comments.GetReplies;

public interface IGetReplies
{
    Task<Validated<RepliesPage>> ExecuteAsync(Guid parentId, int limit = 25, Guid? after = null,
        CancellationToken cancellationToken = default);
}

// Cursor pages bound the number of direct replies, never the depth of the discussion.
public sealed class GetReplies(ICommentQueries queries) : IGetReplies
{
    public async Task<Validated<RepliesPage>> ExecuteAsync(Guid parentId, int limit = 25, Guid? after = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (limit is < 1 or > 100) return Invalid("limit", "Limit must be between 1 and 100.");
        if (await queries.FindByIdAsync(parentId, cancellationToken) is null)
            return Invalid("parentId", "Comment was not found.");
        ReplyCursor? cursor = null;
        if (after is { } id)
        {
            var anchor = await queries.FindByIdAsync(id, cancellationToken);
            if (anchor is null || anchor.ParentId != parentId) return Invalid("after", "Cursor must identify a direct reply to this comment.");
            cursor = new(anchor.CreatedAt, anchor.Id);
        }
        var rows = await queries.GetRepliesAsync(parentId, limit + 1, cursor, cancellationToken);
        return Validated<RepliesPage>.Success(new(rows.Take(limit).ToArray(), rows.Count > limit ? rows[limit - 1].Id : null));
    }

    private static Validated<RepliesPage> Invalid(string field, string message) =>
        Validated<RepliesPage>.Invalid(new() { [field] = [message] });
}
