using CommentsService.Api.DTO.GraphQL;
using CommentsService.Application.Comments.GetComment;
using CommentsService.Application.Comments.GetRootPage;
using CommentsService.Application.Comments.GetReplies;
using CommentsService.Application.Common;
using HotChocolate;

namespace CommentsService.Api.GraphQL;

public sealed class CommentQuery
{
    public async Task<RootPageNode> GetRootPageAsync([Service] IGetRootPage get,
        CancellationToken cancellationToken, int page = 1, string sort = "createdAt", string direction = "desc")
    {
        var result = await get.ExecuteAsync(page, sort, direction, cancellationToken);
        var value = Require(result);
        return new(value.Items.Select(CommentNode.From).ToArray(), value.Page, value.PageSize, value.TotalCount, value.TotalPages);
    }

    public async Task<RepliesNode> GetRepliesAsync(Guid parentId, [Service] IGetReplies get,
        [Service] ApplicationLimits limits, CancellationToken cancellationToken, int? limit = null, Guid? after = null)
    {
        var value = Require(await get.ExecuteAsync(parentId, limit ?? limits.DefaultListLimit, after, cancellationToken));
        return new(value.Items.Select(CommentNode.From).ToArray(), value.NextAfter);
    }

    public async Task<CommentNode?> GetCommentAsync(Guid id, [Service] IGetComment get, CancellationToken cancellationToken)
    {
        var value = await get.ExecuteAsync(id, cancellationToken);
        return value is null ? null : CommentNode.From(value);
    }

    private static T Require<T>(Validated<T> result) where T : class
    {
        if (result.IsValid) return result.Value!;
        throw new GraphQLException(ErrorBuilder.New().SetMessage("Invalid comment query arguments.")
            .SetCode("BAD_USER_INPUT").Build());
    }
}
