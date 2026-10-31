namespace CommentsService.Application.Comments.GetReplies;

public sealed record RepliesPage(IReadOnlyList<CommentDto> Items, Guid? NextAfter);
