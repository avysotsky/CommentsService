using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Application.Common;

namespace CommentsService.Application.Comments.GetRootPage;

public enum CommentSort { UserName, Email, CreatedAt }
public enum SortDirection { Asc, Desc }
public interface IGetRootPage
{
    Task<Validated<RootCommentsPage>> ExecuteAsync(int page = 1, string sort = "createdAt", string direction = "desc",
        CancellationToken cancellationToken = default);
}

public sealed class GetRootPage(ICommentQueries queries) : IGetRootPage
{
    public const int PageSize = 25;
    public const int MaxPage = int.MaxValue / PageSize;
    public async Task<Validated<RootCommentsPage>> ExecuteAsync(int page = 1, string sort = "createdAt", string direction = "desc",
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var errors = new Dictionary<string, string[]>();
        if (page < 1 || page > MaxPage) errors["page"] = [$"Page must be between 1 and {MaxPage}."];
        var field = sort switch { "userName" => CommentSort.UserName, "email" => CommentSort.Email, _ => CommentSort.CreatedAt };
        if (sort is not ("userName" or "email" or "createdAt")) errors["sort"] = ["Sort must be userName, email or createdAt."];
        if (direction is not ("asc" or "desc")) errors["direction"] = ["Direction must be asc or desc."];
        if (errors.Count != 0) return Validated<RootCommentsPage>.Invalid(errors);
        return Validated<RootCommentsPage>.Success(await queries.GetRootPageAsync(new(page, field,
            direction == "asc" ? SortDirection.Asc : SortDirection.Desc), cancellationToken));
    }
}
