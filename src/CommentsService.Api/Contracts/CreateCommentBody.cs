using System.Text.Json.Serialization;

namespace CommentsService.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateCommentBody(string? UserName = null, string? Email = null,
    string? HomePage = null, string? Text = null);
