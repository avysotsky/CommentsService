namespace CommentsService.Application.Events;

// Immutable post-commit fact; deliberately excludes author/client details and content.
public sealed record CommentCreated(Guid EventId, Guid CommentId, Guid? ParentId, Guid RootId, DateTimeOffset CreatedAt);
