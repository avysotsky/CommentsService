using CommentsService.Domain.Entities;

namespace CommentsService.Application.Comments;

public static class CommentMapping
{
    public static CommentDto From(Comment comment) => new(comment.Id, comment.Author.UserName,
        comment.Author.Email, comment.Author.HomePage, comment.Text,
        comment.CreatedAt.AddTicks(-(comment.CreatedAt.Ticks % 10)), comment.ParentId, comment.RootId,
        comment.Image is { } image ? new(image.Id, image.FileName, image.Width, image.Height) : null,
        comment.TextFile is { } file ? new(file.Id, file.FileName, file.SizeBytes) : null);
}
