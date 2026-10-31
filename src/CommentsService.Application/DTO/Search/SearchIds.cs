namespace CommentsService.Application.Search;
public sealed record SearchIds(IReadOnlyList<Guid> Items, bool HasMore);
