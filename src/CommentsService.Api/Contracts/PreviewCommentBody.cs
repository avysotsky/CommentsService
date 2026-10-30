using System.Text.Json.Serialization;
namespace CommentsService.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PreviewCommentBody(string? Text = null);
