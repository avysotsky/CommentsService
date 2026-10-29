using CommentsService.Domain.Entities;

namespace CommentsService.Application.Comments;

// Client network metadata and persistence navigation properties are never public response fields.
public sealed record CommentDto(Guid Id, string UserName, string Email, string? HomePage,
    string Text, DateTimeOffset CreatedAt)
{
    public static CommentDto From(Comment comment) => new(comment.Id, comment.Author.UserName,
        comment.Author.Email, comment.Author.HomePage, comment.Text,
        comment.CreatedAt.AddTicks(-(comment.CreatedAt.Ticks % 10)));
}
