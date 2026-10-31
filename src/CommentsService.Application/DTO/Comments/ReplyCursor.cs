namespace CommentsService.Application.Comments.GetReplies;

public sealed record ReplyCursor(DateTimeOffset CreatedAt, Guid Id);
