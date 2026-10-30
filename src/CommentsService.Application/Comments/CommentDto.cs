using CommentsService.Domain.Entities;

namespace CommentsService.Application.Comments;

// Client network metadata and persistence navigation properties are never public response fields.
public sealed record CommentDto(Guid Id, string UserName, string Email, string? HomePage,
    string Text, DateTimeOffset CreatedAt, Guid? ParentId, Guid RootId, ImageInfo? Image = null, TextFileInfo? TextFile = null)
{
    public static CommentDto From(Comment comment) => new(comment.Id, comment.Author.UserName,
        comment.Author.Email, comment.Author.HomePage, comment.Text,
        comment.CreatedAt.AddTicks(-(comment.CreatedAt.Ticks % 10)), comment.ParentId, comment.RootId, comment.Image is { } image ? new(image.Id, image.FileName, image.Width, image.Height) : null, comment.TextFile is { } file ? new(file.Id, file.FileName, file.SizeBytes) : null);
}

public sealed record ImageInfo(Guid Id, string FileName, int Width, int Height);

public sealed record TextFileInfo(Guid Id, string FileName, int SizeBytes);
