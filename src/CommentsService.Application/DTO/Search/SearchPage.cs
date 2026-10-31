using CommentsService.Application.Comments;
namespace CommentsService.Application.Search;
public sealed record SearchPage(IReadOnlyList<CommentDto> Items, int Page, int PageSize, bool HasMore);
