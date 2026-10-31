namespace CommentsService.Application.Comments.GetRootPage;

public sealed record RootPageQuery(int Page, CommentSort Sort, SortDirection Direction, int PageSize = 25);
