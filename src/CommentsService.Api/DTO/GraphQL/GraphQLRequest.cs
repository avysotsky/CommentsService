using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommentsService.Api.DTO.GraphQL;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record GraphQLRequest(string? Query, JsonElement? Variables, string? OperationName);
