namespace CommentsService.Application.Comments;

// Client network metadata and persistence navigation properties are never public response fields.
public sealed record CommentDto(Guid Id, string UserName, string Email, string? HomePage,
    string Text, DateTimeOffset CreatedAt, Guid? ParentId, Guid RootId, ImageInfo? Image = null, TextFileInfo? TextFile = null);
