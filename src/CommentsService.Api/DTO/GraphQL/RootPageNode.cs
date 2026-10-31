namespace CommentsService.Api.DTO.GraphQL;

public sealed record RootPageNode(IReadOnlyList<CommentNode> Items, int Page, int PageSize, long TotalCount, long TotalPages);
