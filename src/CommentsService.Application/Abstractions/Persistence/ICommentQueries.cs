using CommentsService.Application.Comments;

namespace CommentsService.Application.Abstractions.Persistence;

public interface ICommentQueries
{
    Task<CommentDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommentDto>> GetLatestAsync(int limit, CancellationToken cancellationToken = default);
}
