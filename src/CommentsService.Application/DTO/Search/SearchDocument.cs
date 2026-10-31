namespace CommentsService.Application.Search;
public sealed record SearchDocument(Guid Id, string UserName, string Text, DateTimeOffset CreatedAt);
