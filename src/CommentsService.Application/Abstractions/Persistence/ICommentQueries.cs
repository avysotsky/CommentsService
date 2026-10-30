using CommentsService.Application.Comments;

namespace CommentsService.Application.Abstractions.Persistence;

public interface ICommentQueries
{
    Task<IReadOnlyList<CommentDto>> GetRepliesAsync(Guid parentId, int limit,
        CommentsService.Application.Comments.GetReplies.ReplyCursor? after, CancellationToken cancellationToken = default);
    Task<CommentDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommentDto>> GetLatestAsync(int limit, CancellationToken cancellationToken = default);
}
