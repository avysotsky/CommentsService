using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Application.Common;

namespace CommentsService.Application.Comments.GetLatestComments;

public interface IGetLatestComments
{
    Task<Validated<IReadOnlyList<CommentDto>>> ExecuteAsync(int limit, CancellationToken cancellationToken = default);
}

public sealed class GetLatestComments(ICommentQueries queries) : IGetLatestComments
{
    public const int DefaultLimit = 25;
    public const int MaxLimit = 100;

    public async Task<Validated<IReadOnlyList<CommentDto>>> ExecuteAsync(int limit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (limit is < 1 or > MaxLimit)
            return Validated<IReadOnlyList<CommentDto>>.Invalid(new() { ["limit"] = ["Limit must be between 1 and 100."] });
        return Validated<IReadOnlyList<CommentDto>>.Success(await queries.GetLatestAsync(limit, cancellationToken));
    }
}
