using CommentsService.Application.Comments;

namespace CommentsService.Api.DTO.GraphQL;

// Transport-owned values only: no persistence entities, client network metadata or file bytes.
public sealed record CommentNode(Guid Id, string UserName, string Email, string? HomePage,
    string Text, DateTimeOffset CreatedAt, Guid? ParentId, Guid RootId, ImageNode? Image, TextFileNode? TextFile)
{
    public static CommentNode From(CommentDto value) => new(value.Id, value.UserName, value.Email,
        value.HomePage, value.Text, value.CreatedAt, value.ParentId, value.RootId,
        value.Image is { } image ? new(image.Id, image.FileName, image.Width, image.Height) : null,
        value.TextFile is { } file ? new(file.Id, file.FileName, file.SizeBytes) : null);
}
