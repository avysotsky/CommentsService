using CommentsService.Application.Common;

namespace CommentsService.Application.Comments.CreateComment;

public interface ICreateComment
{
    Task<Validated<CommentDto>> ExecuteAsync(CreateCommentInput input, ClientMetadata client,
        CancellationToken cancellationToken = default);
}
