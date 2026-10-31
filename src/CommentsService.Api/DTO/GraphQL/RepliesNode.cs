namespace CommentsService.Api.DTO.GraphQL;

public sealed record RepliesNode(IReadOnlyList<CommentNode> Items, Guid? NextAfter);
