namespace CommentsService.Application.Comments.GetRootPage;

public sealed record RootCommentsPage(IReadOnlyList<CommentDto> Items, int Page, int PageSize, long TotalCount, long TotalPages);
