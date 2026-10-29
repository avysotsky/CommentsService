using CommentsService.Application.Abstractions.Persistence;

namespace CommentsService.Application.Comments.GetComment;

public interface IGetComment
{
    Task<CommentDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class GetComment(ICommentQueries queries) : IGetComment
{
    public Task<CommentDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default) =>
        queries.FindByIdAsync(id, cancellationToken);
}
