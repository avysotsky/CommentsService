using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Application.Common;

namespace CommentsService.Application.Comments.GetLatestComments;

public interface IGetLatestComments
{
    Task<Validated<IReadOnlyList<CommentDto>>> ExecuteAsync(int limit, CancellationToken cancellationToken = default);
}

public sealed class GetLatestComments(ICommentQueries queries, ApplicationLimits? settings = null) : IGetLatestComments
{
    private readonly ApplicationLimits limits = settings ?? new();

    public async Task<Validated<IReadOnlyList<CommentDto>>> ExecuteAsync(int limit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (limit < 1 || limit > limits.MaximumListLimit)
            return Validated<IReadOnlyList<CommentDto>>.Invalid(new() { ["limit"] = [$"Limit must be between 1 and {limits.MaximumListLimit}."] });
        return Validated<IReadOnlyList<CommentDto>>.Success(await queries.GetLatestAsync(limit, cancellationToken));
    }
}
