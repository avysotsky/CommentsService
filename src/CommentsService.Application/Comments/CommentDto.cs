using CommentsService.Domain.Entities;

namespace CommentsService.Application.Comments;

// Client network metadata and persistence navigation properties are never public response fields.
public sealed record CommentDto(Guid Id, string UserName, string Email, string? HomePage,
    string Text, DateTimeOffset CreatedAt, Guid? ParentId, Guid RootId, ImageInfo? Image = null)
{
    public static CommentDto From(Comment comment) => new(comment.Id, comment.Author.UserName,
        comment.Author.Email, comment.Author.HomePage, comment.Text,
        comment.CreatedAt.AddTicks(-(comment.CreatedAt.Ticks % 10)), comment.ParentId, comment.RootId, comment.Image is { } image ? new(image.Id, image.FileName, image.Width, image.Height) : null);
}

public sealed record ImageInfo(Guid Id, string FileName, int Width, int Height);
