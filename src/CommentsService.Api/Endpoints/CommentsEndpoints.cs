using System.Net;
using CommentsService.Api.Contracts;
using CommentsService.Application.Comments.CreateComment;
using CommentsService.Application.Comments.GetComment;
using CommentsService.Application.Comments.GetLatestComments;

namespace CommentsService.Api.Endpoints;

public static class CommentsEndpoints
{
    public static IEndpointRouteBuilder MapCommentsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/comments");
        group.MapPost("", async (HttpContext context, ICreateComment create, CancellationToken token) =>
        {
            if (!context.Request.HasJsonContentType()) return Results.Problem(
                statusCode: StatusCodes.Status415UnsupportedMediaType, title: "Unsupported media type.");
            var body = await context.Request.ReadFromJsonAsync<CreateCommentBody>(token);
            if (body is null) return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid request body.");
            var ip = context.Connection.RemoteIpAddress ?? IPAddress.Any;
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var client = new ClientMetadata(ip, userAgent[..Math.Min(userAgent.Length, ClientMetadata.UserAgentMaxLength)]);
            var result = await create.ExecuteAsync(new(body.UserName, body.Email, body.HomePage, body.Text), client, token);
            if (!result.IsValid) return Results.ValidationProblem(result.Errors.ToDictionary());
            return Results.Created($"/api/comments/{result.Value!.Id}", result.Value);
        });
        group.MapGet("/{id:guid}", async (Guid id, IGetComment get, CancellationToken token) =>
            await get.ExecuteAsync(id, token) is { } comment ? Results.Ok(comment) : Results.Problem(
                statusCode: StatusCodes.Status404NotFound, title: "Comment was not found."));
        group.MapGet("", async (int? limit, IGetLatestComments get, CancellationToken token) =>
        {
            var result = await get.ExecuteAsync(limit ?? GetLatestComments.DefaultLimit, token);
            return result.IsValid ? Results.Ok(result.Value) : Results.ValidationProblem(result.Errors.ToDictionary());
        });
        return endpoints;
    }
}
